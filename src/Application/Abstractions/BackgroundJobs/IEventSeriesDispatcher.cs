namespace Sportner.Application.Abstractions.BackgroundJobs;

public interface IEventSeriesDispatcher
{
    /// <summary>
    /// Bitmiş son halkası olan tekrarlayan serilerde bir sonraki etkinliği açar.
    /// Serideki tekrarlar önden toplu üretilmez; her biri bir öncekinin
    /// planlanan bitişinden sonra oluşur. Oluşturulan etkinlik sayısını döner.
    /// </summary>
    Task<int> DispatchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tek bir serinin sıradaki halkasını, vakti geldiyse açar. Idempotent:
    /// serinin son halkası henüz bitmediyse veya kota dolduysa null döner.
    /// </summary>
    Task<Guid?> CreateNextIfDueAsync(Guid seriesId, CancellationToken cancellationToken = default);
}
