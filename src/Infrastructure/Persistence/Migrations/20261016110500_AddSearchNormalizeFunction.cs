using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Arama karşılaştırmalarının Türkçe metinde doğru çalışması için
    /// sadeleştirme fonksiyonu. <c>SearchText.Normalize</c> EF tarafından bu
    /// fonksiyona çevriliyor; iki tarafın aynı sonucu üretmesi şart.
    ///
    /// <c>lower()</c> tek başına yetmiyordu: "I" ile "İ" ayrı harfler olduğu
    /// için klavyenin büyüttüğü "Istanbul" kayıtlı "İstanbul" ile eşleşmiyordu,
    /// ayrıca Türkçe klavyesi olmayan kullanıcı "yuzme" yazıp "Yüzme"yi
    /// bulamıyordu.
    ///
    /// unaccent eklentisi yerine translate() tercih edildi: eklenti kurmayı
    /// gerektirmiyor, noktasız ı'yı da kapsıyor ve unaccent yalnızca STABLE
    /// olduğu için ifade indekslerinde doğrudan kullanılamıyor.
    /// </summary>
    public partial class AddSearchNormalizeFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public.sportner_search_normalize(value text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                PARALLEL SAFE
                RETURNS NULL ON NULL INPUT
                AS $$
                    SELECT btrim(lower(translate(value,
                        'ÇçĞğİıÖöŞşÜüÂâÎîÛû',
                        'CcGgIiOoSsUuAaIiUu')))
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP FUNCTION IF EXISTS public.sportner_search_normalize(text);");
        }
    }
}
