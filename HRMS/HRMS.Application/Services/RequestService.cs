using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Services
{
    public class RequestService : IRequestService
    {
        private readonly IApplicationDbContext _db;
        private readonly DbConcurrencyGate _gate;
        private readonly IEmployeeLookup _employeeLookup;
        private readonly IRequestNotifier _notifier;

        public RequestService(
            IApplicationDbContext db,
            DbConcurrencyGate gate,
            IEmployeeLookup employeeLookup,
            IRequestNotifier notifier)
        {
            _db = db;
            _gate = gate;
            _employeeLookup = employeeLookup;
            _notifier = notifier;
        }

        // =====================================================================
        // 1. SUBMIT REQUEST
        // =====================================================================
        public async Task<int> SubmitRequestAsync(RequestDto model)
        {
            if (model.AccountId <= 0)
                throw new RequestWorkflowException("Thiếu thông tin tài khoản.");

            if (model.RequestTypeId <= 0)
                throw new RequestWorkflowException("Vui lòng chọn loại đơn.");

            Request entity;
            RequestType requestType;

            await using (await _gate.EnterAsync())
            {
                requestType = await _db.RequestTypes.FirstOrDefaultAsync(t => t.Id == model.RequestTypeId)
                    ?? throw new RequestWorkflowException("Loại đơn không hợp lệ.");

                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == model.AccountId)
                    ?? throw new RequestWorkflowException("Không tìm thấy tài khoản.");

                DateTime startDate;
                DateTime endDate;
                decimal value;

                switch (requestType.Code.ToUpperInvariant())
                {
                    case RequestTypeCodes.Leave:
                        (startDate, endDate, value) = await ResolveLeaveAsync(model, account.UserId);
                        break;

                    case RequestTypeCodes.Overtime:
                        (startDate, endDate, value) = ResolveOvertime(model);
                        break;

                    case RequestTypeCodes.Complaint:
                        (startDate, endDate, value) = ResolveComplaint(model);
                        break;

                    default:
                        throw new RequestWorkflowException(
                            $"Loại đơn '{requestType.Name}' (Code={requestType.Code}) chưa được hỗ trợ xử lý nghiệp vụ.");
                }

                var isSubmitting = model.SubmitImmediately;

                entity = new Request
                {
                    Title = string.IsNullOrWhiteSpace(model.Title) ? BuildDefaultTitle(requestType.Code, model) : model.Title!.Trim(),
                    Reason = model.Reason,
                    Status = isSubmitting ? RequestStatuses.Pending : RequestStatuses.Draft,
                    StartDate = startDate,
                    EndDate = endDate,
                    Value = value,
                    CreatedAt = DateTime.Now,
                    RequestTypeId = requestType.Id,
                    CreatedByAccountId = model.AccountId,
                    CurrentApproverAccountId = isSubmitting
                        ? await ResolveApproverAsync(requestType.Code, model.AccountId)
                        : null
                };

                _db.Requests.Add(entity);
                await _db.SaveChangesAsync();
            }

            if (entity.Status == RequestStatuses.Pending && entity.CurrentApproverAccountId is not null)
            {
                var dto = await ToListItemDtoAsync(entity, requestType);
                await _notifier.NotifyNewRequestAsync(dto);
            }

            return entity.Id;
        }

        /// <summary>Nghỉ phép: kiểm tra số ngày phép còn lại (LeaveBalances khoá theo Users.Id, không phải Accounts.Id).</summary>
        private async Task<(DateTime start, DateTime end, decimal value)> ResolveLeaveAsync(RequestDto model, int userId)
        {
            if (model.LeaveStartDate is null || model.LeaveEndDate is null)
                throw new RequestWorkflowException("Vui lòng chọn ngày bắt đầu và kết thúc nghỉ phép.");

            if (model.LeaveEndDate < model.LeaveStartDate)
                throw new RequestWorkflowException("Ngày kết thúc phải sau ngày bắt đầu.");

            var start = model.LeaveStartDate.Value;
            var end = model.LeaveEndDate.Value;

            var requestedDays = end.DayNumber - start.DayNumber + 1;
            if (requestedDays <= 0)
                throw new RequestWorkflowException("Khoảng ngày nghỉ không hợp lệ.");

            var year = start.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Year == year)
                ?? throw new RequestWorkflowException($"Không tìm thấy dữ liệu phép năm {year} cho nhân viên này.");

            var pendingDays = await _db.Requests
                .Where(r => r.CreatedByAccountId == model.AccountId 
                            && r.Status == RequestStatuses.Pending 
                            && r.RequestType.Code == RequestTypeCodes.Leave
                            && r.StartDate.Year == year)
                .SumAsync(r => r.Value);

            var availableDays = balance.RemainingDays - pendingDays;
            if (availableDays < requestedDays)
                throw new RequestWorkflowException(
                    $"Không đủ số ngày phép khả dụng. Bạn còn {balance.RemainingDays} ngày phép, nhưng có {pendingDays} ngày đang chờ phê duyệt. Số ngày khả dụng thực tế hiện tại là {availableDays} ngày.");

            return (start.ToDateTime(TimeOnly.MinValue), end.ToDateTime(TimeOnly.MinValue), requestedDays);
        }

        private static (DateTime start, DateTime end, decimal value) ResolveOvertime(RequestDto model)
        {
            if (model.OvertimeDate is null || model.OvertimeHours is null || model.OvertimeHours <= 0)
                throw new RequestWorkflowException("Vui lòng nhập ngày làm OT và số giờ OT hợp lệ.");

            var date = model.OvertimeDate.Value.ToDateTime(TimeOnly.MinValue);
            return (date, date, model.OvertimeHours.Value);
        }

        private static (DateTime start, DateTime end, decimal value) ResolveComplaint(RequestDto model)
        {
            if (model.ComplaintWorkDate is null || model.ComplaintProposedHours is null)
                throw new RequestWorkflowException("Vui lòng nhập ngày công cần khiếu nại và số giờ đề nghị điều chỉnh.");

            var date = model.ComplaintWorkDate.Value.ToDateTime(TimeOnly.MinValue);
            return (date, date, model.ComplaintProposedHours.Value);
        }

        private static string BuildDefaultTitle(string typeCode, RequestDto model) => typeCode.ToUpperInvariant() switch
        {
            RequestTypeCodes.Leave => $"Đơn xin nghỉ phép {model.LeaveStartDate:dd/MM/yyyy} - {model.LeaveEndDate:dd/MM/yyyy}",
            RequestTypeCodes.Overtime => $"Đơn làm thêm giờ ngày {model.OvertimeDate:dd/MM/yyyy}",
            RequestTypeCodes.Complaint => $"Đơn khiếu nại công ngày {model.ComplaintWorkDate:dd/MM/yyyy}",
            _ => "Đơn từ"
        };

        /// <summary>
        /// Định tuyến người duyệt theo loại đơn:
        /// - Nghỉ phép: bỏ qua trưởng phòng, đi thẳng tới Trưởng phòng Nhân sự (Department Code=HR).
        /// - OT/Khiếu nại công: về trưởng phòng của người tạo đơn.
        /// </summary>
        private async Task<int?> ResolveApproverAsync(string requestTypeCode, int requesterAccountId)
        {
            if (requestTypeCode.ToUpperInvariant() == RequestTypeCodes.Leave)
            {
                var hrApproverId = await _employeeLookup.GetHrApproverAccountIdAsync();
                return hrApproverId is not null && hrApproverId != requesterAccountId ? hrApproverId : null;
            }

            return await _employeeLookup.GetManagerIdAsync(requesterAccountId);
        }

        // =====================================================================
        // 2. APPROVE REQUEST
        // =====================================================================
        public async Task ApproveRequestAsync(int requestId, int approverAccountId, string? note = null)
        {
            Request entity;

            await using (await _gate.EnterAsync())
            {
                entity = await _db.Requests
                    .Include(r => r.RequestType)
                    .FirstOrDefaultAsync(r => r.Id == requestId)
                    ?? throw new RequestWorkflowException("Không tìm thấy đơn.");

                if (entity.Status != RequestStatuses.Pending)
                    throw new RequestWorkflowException("Chỉ có thể duyệt đơn đang ở trạng thái Chờ duyệt (Pending).");

                if (!await IsAuthorizedApproverAsync(entity, approverAccountId))
                    throw new RequestWorkflowException("Bạn không phải là người được phân công duyệt đơn này.");

                entity.Status = RequestStatuses.Approved;

                var code = entity.RequestType?.Code?.ToUpperInvariant();

                if (code == RequestTypeCodes.Leave)
                {
                    var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == entity.CreatedByAccountId)
                        ?? throw new RequestWorkflowException("Không tìm thấy tài khoản của người tạo đơn.");

                    var year = entity.StartDate.Year;
                    var balance = await _db.LeaveBalances
                        .FirstOrDefaultAsync(b => b.UserId == account.UserId && b.Year == year)
                        ?? throw new RequestWorkflowException($"Không tìm thấy dữ liệu phép năm {year} cho nhân viên này.");

                    var days = (int)entity.Value;

                    if (balance.RemainingDays < days)
                        throw new RequestWorkflowException("Số ngày phép còn lại không đủ tại thời điểm duyệt.");

                    balance.UsedDays += days;
                    balance.RemainingDays -= days;
                }

                await _db.SaveChangesAsync();
            }

            var dto = await ToListItemDtoAsync(entity, entity.RequestType);
            await _notifier.NotifyRequestProcessedAsync(entity.CreatedByAccountId, dto);
        }

        // =====================================================================
        // 3. REJECT REQUEST
        // =====================================================================
        public async Task RejectRequestAsync(int requestId, int approverAccountId, string? note = null)
        {
            Request entity;

            await using (await _gate.EnterAsync())
            {
                entity = await _db.Requests
                    .Include(r => r.RequestType)
                    .FirstOrDefaultAsync(r => r.Id == requestId)
                    ?? throw new RequestWorkflowException("Không tìm thấy đơn.");

                if (entity.Status != RequestStatuses.Pending)
                    throw new RequestWorkflowException("Chỉ có thể từ chối đơn đang ở trạng thái Chờ duyệt (Pending).");

                if (!await IsAuthorizedApproverAsync(entity, approverAccountId))
                    throw new RequestWorkflowException("Bạn không phải là người được phân công duyệt đơn này.");

                entity.Status = RequestStatuses.Rejected;

                await _db.SaveChangesAsync();
            }

            var dto = await ToListItemDtoAsync(entity, entity.RequestType);
            await _notifier.NotifyRequestProcessedAsync(entity.CreatedByAccountId, dto);
        }

        /// <summary>Được phép duyệt/từ chối nếu đúng người được phân công, HOẶC là Giám đốc (duyệt được mọi đơn).</summary>
        private async Task<bool> IsAuthorizedApproverAsync(Request entity, int approverAccountId)
        {
            if (entity.CurrentApproverAccountId == approverAccountId)
                return true;

            var directorId = await _employeeLookup.GetDirectorAccountIdAsync();
            return directorId is not null && directorId == approverAccountId;
        }

        // =====================================================================
        // 4. CANCEL REQUEST (nhân viên tự huỷ)
        // =====================================================================
        public async Task CancelRequestAsync(int requestId, int accountId)
        {
            await using (await _gate.EnterAsync())
            {
                var entity = await _db.Requests
                    .FirstOrDefaultAsync(r => r.Id == requestId && r.CreatedByAccountId == accountId)
                    ?? throw new RequestWorkflowException("Không tìm thấy đơn.");

                if (entity.Status != RequestStatuses.Draft && entity.Status != RequestStatuses.Pending)
                    throw new RequestWorkflowException("Chỉ có thể huỷ đơn khi đang ở trạng thái Nháp hoặc Chờ duyệt.");

                entity.Status = RequestStatuses.Cancelled;

                await _db.SaveChangesAsync();
            }
        }

        // =====================================================================
        // 5. QUERIES
        // =====================================================================
        public async Task<List<RequestListItemDto>> GetMyRequestsAsync(int accountId)
        {
            List<Request> items;

            await using (await _gate.EnterAsync())
            {
                items = await _db.Requests
                    .Include(r => r.RequestType)
                    .Where(r => r.CreatedByAccountId == accountId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }

            var result = new List<RequestListItemDto>();
            foreach (var item in items)
                result.Add(await ToListItemDtoAsync(item, item.RequestType));

            return result;
        }

        public async Task<List<RequestListItemDto>> GetPendingApprovalsAsync(int approverAccountId)
        {
            var directorId = await _employeeLookup.GetDirectorAccountIdAsync();
            var isDirector = directorId is not null && directorId == approverAccountId;

            List<Request> pending;

            await using (await _gate.EnterAsync())
            {
                var query = _db.Requests.Include(r => r.RequestType).Where(r => r.Status == RequestStatuses.Pending);

                if (!isDirector)
                    query = query.Where(r => r.CurrentApproverAccountId == approverAccountId);

                pending = await query.OrderBy(r => r.CreatedAt).ToListAsync();
            }

            var result = new List<RequestListItemDto>();
            foreach (var item in pending)
                result.Add(await ToListItemDtoAsync(item, item.RequestType));

            return result;
        }

        public async Task<List<RequestListItemDto>> GetProcessedApprovalsAsync(int approverAccountId)
        {
            var directorId = await _employeeLookup.GetDirectorAccountIdAsync();
            var isDirector = directorId is not null && directorId == approverAccountId;

            List<Request> processed;

            await using (await _gate.EnterAsync())
            {
                var query = _db.Requests
                    .Include(r => r.RequestType)
                    .Where(r => r.Status == RequestStatuses.Approved || r.Status == RequestStatuses.Rejected);

                if (!isDirector)
                    query = query.Where(r => r.CurrentApproverAccountId == approverAccountId);

                processed = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            }

            var result = new List<RequestListItemDto>();
            foreach (var item in processed)
                result.Add(await ToListItemDtoAsync(item, item.RequestType));

            return result;
        }

        public async Task<List<RequestTypeDto>> GetRequestTypesAsync()
        {
            await using (await _gate.EnterAsync())
            {
                return await _db.RequestTypes
                    .OrderBy(t => t.Name)
                    .Select(t => new RequestTypeDto { Id = t.Id, Code = t.Code, Name = t.Name })
                    .ToListAsync();
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================
        private async Task<RequestListItemDto> ToListItemDtoAsync(Request entity, RequestType? requestType)
        {
            if (requestType is null)
            {
                await using (await _gate.EnterAsync())
                {
                    requestType = await _db.RequestTypes.FirstOrDefaultAsync(t => t.Id == entity.RequestTypeId);
                }
            }

            return new RequestListItemDto
            {
                Id = entity.Id,
                AccountId = entity.CreatedByAccountId,
                AccountName = await _employeeLookup.GetEmployeeNameAsync(entity.CreatedByAccountId),
                RequestTypeId = entity.RequestTypeId,
                RequestTypeCode = requestType?.Code ?? string.Empty,
                RequestTypeName = requestType?.Name ?? string.Empty,
                Status = entity.Status,
                Title = entity.Title,
                Reason = entity.Reason,
                Summary = BuildSummary(entity, requestType?.Code),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Value = entity.Value,
                ApproverAccountId = entity.CurrentApproverAccountId,
                CreatedAt = entity.CreatedAt
            };
        }

        private static string BuildSummary(Request entity, string? typeCode) => typeCode?.ToUpperInvariant() switch
        {
            RequestTypeCodes.Leave =>
                $"Nghỉ phép {entity.StartDate:dd/MM/yyyy} - {entity.EndDate:dd/MM/yyyy} ({entity.Value} ngày)",
            RequestTypeCodes.Overtime =>
                $"OT ngày {entity.StartDate:dd/MM/yyyy} - {entity.Value} giờ",
            RequestTypeCodes.Complaint =>
                $"Khiếu nại công ngày {entity.StartDate:dd/MM/yyyy} - đề nghị {entity.Value} giờ",
            _ => entity.Title
        };
    }
}