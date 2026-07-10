namespace Dtrl.Models;

public enum RiskLevel
{
    Safe,       // 🟢 Safe: Recommended, minor impact on system compatibility.
    Advanced,   // 🟡 Advanced: Modifies standard behavior, user should understand changes.
    Dangerous   // 🔴 Dangerous: High risk of reducing compatibility, requires confirmation.
}
