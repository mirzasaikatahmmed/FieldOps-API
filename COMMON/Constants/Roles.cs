namespace FieldOps.COMMON.Constants;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string Dispatcher = "Dispatcher";
    public const string Technician = "Technician";

    public static readonly string[] All =
    [
        SuperAdmin,
        CompanyAdmin,
        Dispatcher,
        Technician
    ];

    public static readonly string[] CompanyRoles =
    [
        CompanyAdmin,
        Dispatcher,
        Technician
    ];
}
