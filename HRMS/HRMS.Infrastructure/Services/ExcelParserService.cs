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

    public async Task<List<AttendanceRowDto>> ParseAsync(Stream fileStream)
    {

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var result = new List<AttendanceRowDto>();

        using var memoryStream = new System.IO.MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var package = new ExcelPackage(memoryStream);

        var sheet = package.Workbook.Worksheets.FirstOrDefault();
        if (sheet == null || sheet.Dimension == null)
            return result; 

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

        if (colEmployeeCode == -1 || colCheckedAt == -1 || colCheckType == -1)
            return result;

        int totalRows = sheet.Dimension.End.Row;

        for (int row = 2; row <= totalRows; row++)
        {
            var employeeCode = sheet.Cells[row, colEmployeeCode].Text.Trim();
            var checkTypeRaw = sheet.Cells[row, colCheckType].Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(employeeCode)) continue;

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
                    continue;
                }
            }

            result.Add(new AttendanceRowDto
            {
                EmployeeCode = employeeCode,
                CheckedAt    = checkedAt,
                CheckType    = checkTypeRaw 
            });
        }

        return result;
    }
}
