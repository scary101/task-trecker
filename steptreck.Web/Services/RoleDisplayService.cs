using steptreck.Domain.Enums;

namespace steptreck.Web.Services
{
    public static class RoleDisplayService
    {
        public static string GetRoleLabel(RoleEnum role) => role switch
        {
            RoleEnum.Owner => "Руководитель",
            RoleEnum.Admin => "администратор",
            RoleEnum.TeamLead => "ТимЛид",
            RoleEnum.Employee => "Сотрудник",
            RoleEnum.ProjectManaget => "Менеджер проекта",
            _ => role.ToString()
        };

        public static string GetRoleLabel(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "—";

            return role.Trim().ToLowerInvariant() switch
            {
                "owner" => "Руководитель",
                "admin" => "администратор",
                "teamlead" => "ТимЛид",
                "team lead" => "ТимЛид",
                "employee" => "Сотрудник",
                "projectmanaget" => "Менеджер проекта",
                "projectmanager" => "Менеджер проекта",
                "владелец" => "Руководитель",
                "руководитель" => "Руководитель",
                "администратор" => "администратор",
                "тимлид" => "ТимЛид",
                "сотрудник" => "Сотрудник",
                "менеджер проекта" => "Менеджер проекта",
                _ => role
            };
        }
    }
}
