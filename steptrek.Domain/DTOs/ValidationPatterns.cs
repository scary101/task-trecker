using System;

namespace steptreck.Domain.DTOs
{
    public static class ValidationPatterns
    {
        // Person names: letters (latin/cyrillic), spaces, hyphen, apostrophe.
        public const string PersonName = @"^[A-Za-zА-Яа-яЁё][A-Za-zА-Яа-яЁё\-\s']{1,49}$";

        // Organization / project / team names: letters, digits, space, dash, underscore, dot.
        public const string OrgTeamProjectName = @"^[\p{L}0-9][\p{L}0-9\s\-_.]{1,59}$";

        // Role names: letters, spaces, dash.
        public const string RoleName = @"^[\p{L}][\p{L}\s\-]{1,49}$";

        // Titles: allow letters, digits, spaces, and common punctuation.
        public const string ShortTitle = @"^[\p{L}0-9][\p{L}0-9\s\-_,.!?:;()""'#/]{0,119}$";
        public const string ChecklistTitle = @"^[\p{L}0-9][\p{L}0-9\s\-_,.!?:;()""'#/]{0,79}$";

        public const string Code = @"^\d{4,8}$";
        public const string Token = @"^[A-Za-z0-9_\-+=/\.]+$";

        // Strong password: 8-64, upper, lower, digit, special.
        public const string PasswordStrong = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,64}$";
    }
}
