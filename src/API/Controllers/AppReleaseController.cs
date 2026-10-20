using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sportner.API.Controllers;

/// <summary>
/// Uygulamanın açılışta sorduğu sürüm eşiği.
///
/// Günlük güncellemeler OTA ile sessizce iniyor; bu uç o yolun kapalı olduğu
/// durum için: native bir değişiklik ya da geriye dönük uyumsuz bir API
/// değişikliği olduğunda eski istemciyi durdurmak gerekiyor. Normalde hiç
/// devreye girmez — eşik yayındaki en düşük çalışan sürümde tutulur.
///
/// Kimlik doğrulaması yok: eski istemci giriş yapamıyor olabilir, "güncelle"
/// ekranını yine de görmesi gerekiyor.
/// </summary>
[ApiController]
[Route("api/app")]
[AllowAnonymous]
public sealed class AppReleaseController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppReleaseController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("release")]
    public IActionResult Release([FromQuery] string? platform)
    {
        var isAndroid = string.Equals(platform, "android", StringComparison.OrdinalIgnoreCase);

        var minimumVersion = isAndroid
            ? _configuration["AppRelease:MinimumAndroidVersion"]
            : _configuration["AppRelease:MinimumIosVersion"];

        var storeUrl = isAndroid
            ? _configuration["AppLinks:AndroidStoreUrl"]
            : _configuration["AppLinks:IosStoreUrl"];

        return Ok(new AppReleaseResponse(minimumVersion, storeUrl));
    }

    /// <param name="MinimumVersion">
    /// Boş gelebilir: eşik tanımlı değilse istemci hiçbir şey yapmaz.
    /// Zorunluluğu yapılandırmadan açıp kapatabilmek için kasıtlı.
    /// </param>
    public sealed record AppReleaseResponse(string? MinimumVersion, string? StoreUrl);
}
