namespace Sportner.Application.Abstractions.Authentication;

/// <summary>Yönetici izin listesine erişim (yapılandırmadaki <c>Authorization:AdminUserIds</c>).</summary>
public interface IAdminDirectory
{
    bool IsAdmin(Guid userId);
}
