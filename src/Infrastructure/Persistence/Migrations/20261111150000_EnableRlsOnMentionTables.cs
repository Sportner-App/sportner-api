using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sportner.Infrastructure.Persistence;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Etiket tabloları RLS'siz oluşturulmuştu; Supabase'in herkese açık REST katmanı
    /// (anon anahtarla) bu tabloları okuyabiliyordu. API service bağlantısıyla çalıştığı
    /// için RLS'i açmak uygulamayı etkilemez.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261111150000_EnableRlsOnMentionTables")]
    public partial class EnableRlsOnMentionTables : Migration
    {
        private static readonly string[] Tables =
            ["PostMentions", "CommentMentions", "MessageMentions", "EventQuestionMentions"];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
