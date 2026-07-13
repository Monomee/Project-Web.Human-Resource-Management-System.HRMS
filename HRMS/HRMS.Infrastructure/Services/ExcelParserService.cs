using OfficeOpenXml;
using HRMS.Application.DTOs.Attendance;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Dịch vụ đọc file Excel từ máy chấm công, sử dụng thư viện EPPlus.
/// Lớp này implement IExcelParserService (được định nghĩa ở tầng Application).
/// </summary>
public class ExcelParserService : IExcelParserService
{
    /// <summary>
    /// Đọc luồng Stream của file .xlsx và trả về danh sách các bản ghi chấm công thô.
    ///
    /// CẤU TRÚC FILE EXCEL KỲ VỌNG:
    ///   Dòng 1 (header): EmployeeCode | CheckedAt | CheckType
    ///   Dòng 2 trở đi : NV001        | 2024-07-01 08:02 | IN
    ///                   NV001        | 2024-07-01 17:45 | OUT
    ///                   ...
    /// </summary>
    public async Task<List<AttendanceRowDto>> ParseAsync(Stream fileStream)
    {
        // EPPlus v5+ yêu cầu khai báo license context.
        // NonCommercial = dùng cho học tập / nội bộ, hoàn toàn miễn phí.
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var result = new List<AttendanceRowDto>();

        using var memoryStream = new System.IO.MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var package = new ExcelPackage(memoryStream);

        // Lấy sheet đầu tiên trong workbook
        var sheet = package.Workbook.Worksheets.FirstOrDefault();
        if (sheet == null || sheet.Dimension == null)
            return result; // File rỗng → trả về list rỗng

        // Đọc dòng header (dòng 1) để xác định vị trí (index) của từng cột.
        // Cách này cho phép file Excel có cột theo thứ tự bất kỳ.
        int colEmployeeCode = -1, colCheckedAt = -1, colCheckType = -1;

        int totalCols = sheet.Dimension.End.Column;
        for (int col = 1; col <= totalCols; col++)
        {
            var header = sheet.Cells[1, col].Text.Trim();
            switch (header)
            {
                case "EmployeeCode": colEmployeeCode = col; break;
                case "CheckedAt":    colCheckedAt    = col; break;
                case "CheckType":    colCheckType    = col; break;
            }
        }

        // Nếu file Excel không có đủ 3 cột bắt buộc → trả về rỗng
        if (colEmployeeCode == -1 || colCheckedAt == -1 || colCheckType == -1)
            return result;

        int totalRows = sheet.Dimension.End.Row;

        // Duyệt từ dòng 2 (bỏ qua header dòng 1)
        for (int row = 2; row <= totalRows; row++)
        {
            var employeeCode = sheet.Cells[row, colEmployeeCode].Text.Trim();
            var checkTypeRaw = sheet.Cells[row, colCheckType].Text.Trim().ToUpper();

            // Bỏ qua dòng rỗng
            if (string.IsNullOrEmpty(employeeCode)) continue;

            // Parse cột CheckedAt — thử nhiều chiến lược:
            // 1. Nếu EPPlus tự nhận diện được kiểu DateTime
            // 2. Lấy giá trị số từ cell (Excel lưu DateTime dưới dạng số OADate)
            // 3. Parse chuỗi text theo định dạng dd/MM/yyyy HH:mm (ngày/tháng/năm giờ:phút)
            DateTime checkedAt;
            var checkedAtCell = sheet.Cells[row, colCheckedAt];
            var cellValue = checkedAtCell.Value;

            if (cellValue is DateTime dt)
            {
                checkedAt = dt;
            }
            else if (cellValue is double oaDate)
            {
                checkedAt = DateTime.FromOADate(oaDate);
            }
            else
            {
                var text = checkedAtCell.Text.Trim();
                string[] formats = { 
                    "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/M/yyyy HH:mm", "d/M/yyyy HH:mm", "d/MM/yyyy HH:mm",
                    "dd/MM/yyyy H:mm", "d/M/yyyy H:mm", "dd/M/yyyy H:mm",
                    "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy HH:mm", "dd-M-yyyy HH:mm", "d-M-yyyy HH:mm", "d-MM-yyyy HH:mm",
                    "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd HH:mm"
                };

                if (!DateTime.TryParseExact(text, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out checkedAt)
                    && !DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out checkedAt)
                    && !DateTime.TryParse(text, out checkedAt))
                {
                    // Nếu không parse được → bỏ qua dòng này
                    continue;
                }
            }

            result.Add(new AttendanceRowDto
            {
                EmployeeCode = employeeCode,
                CheckedAt    = checkedAt,
                CheckType    = checkTypeRaw // "IN" hoặc "OUT"
            });
        }

        return result;
    }
}
