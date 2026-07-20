namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Code của 2 phòng ban có vai trò đặc biệt trong quy trình duyệt đơn:
    /// - BOD (Ban Giám đốc): HeadAccountId = Giám đốc, được duyệt TẤT CẢ đơn của mọi phòng ban.
    /// - HR (Phòng Nhân sự): HeadAccountId = Trưởng phòng Nhân sự, được duyệt TẤT CẢ đơn nghỉ phép
    ///   của toàn công ty (đơn nghỉ phép bỏ qua trưởng phòng, đi thẳng tới đây).
    /// </summary>
    public static class DepartmentCodes
    {
        public const string Director = "BOD";
        public const string Hr = "HR";
    }
}