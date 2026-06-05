using Microsoft.EntityFrameworkCore;
using steptreck.API.Models;
using System.Reflection;
using System.Text;

namespace steptreck.API.Services.ImportExport
{
    public class ImportExportDataService
    {
        private readonly AppDbContext _context;

        public ImportExportDataService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> ExportTableToCsvAsync(string tableName)
        {
            var entityType = _context.Model.GetEntityTypes()
                .FirstOrDefault(e => e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true);

            if (entityType == null)
                throw new InvalidOperationException($"Таблица '{tableName}' не найдена в контексте EF.");

            var clrType = entityType.ClrType;
            var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!;
            var genericSet = setMethod.MakeGenericMethod(clrType);
            var dbSet = genericSet.Invoke(_context, null)!;

            var queryable = (IQueryable)dbSet;
            var data = await EntityFrameworkQueryableExtensions.ToListAsync(
                (dynamic)queryable
            );

            var sb = new StringBuilder();
            var props = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            sb.AppendLine($"# {tableName}");
            sb.AppendLine(string.Join(",", props.Select(p => p.Name)));

            foreach (var item in data)
            {
                var values = props.Select(p => EscapeCsv(p.GetValue(item)));
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }

        public async Task ImportCsvToTableAsync(string tableName, string csvContent)
        {
            var entityType = _context.Model.GetEntityTypes()
                .FirstOrDefault(e => e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true);

            if (entityType == null)
                throw new InvalidOperationException($"Таблица '{tableName}' не найдена в контексте EF.");

            var clrType = entityType.ClrType;

            var lines = csvContent
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.TrimStart().StartsWith("#"))
                .ToArray();

            if (lines.Length < 2)
                throw new InvalidOperationException("CSV-файл не содержит данных.");

            var headers = lines[0].Split(',').Select(h => h.Trim('"', '\r')).ToArray();
            var props = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var newEntities = new List<object>();

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',').Select(v => v.Trim('"', '\r')).ToArray();
                var entity = Activator.CreateInstance(clrType)!;

                for (int i = 0; i < headers.Length; i++)
                {
                    var prop = props.FirstOrDefault(p => p.Name.Equals(headers[i], StringComparison.OrdinalIgnoreCase));
                    if (prop == null || i >= values.Length) continue;

                    if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string)) continue;

                    if (!string.IsNullOrWhiteSpace(values[i]))
                    {
                        var converted = Convert.ChangeType(values[i], Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                        prop.SetValue(entity, converted);
                    }
                }

                newEntities.Add(entity);
            }
            await _context.AddRangeAsync(newEntities);
            await _context.SaveChangesAsync();
        }

        public async Task<string> ExecuteSqlAsync(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL-запрос не может быть пустым.");

            await using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var isSelect = sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

            if (isSelect)
            {
                var sb = new StringBuilder();
                await using var reader = await command.ExecuteReaderAsync();

                var columns = Enumerable.Range(0, reader.FieldCount)
                                        .Select(reader.GetName)
                                        .ToList();

                sb.AppendLine(string.Join(" | ", columns));
                sb.AppendLine(new string('-', columns.Sum(c => c.Length + 3)));

                while (await reader.ReadAsync())
                {
                    var values = Enumerable.Range(0, reader.FieldCount)
                                           .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString())
                                           .ToList();
                    sb.AppendLine(string.Join(" | ", values));
                }

                return sb.ToString();
            }
            else
            {
                var affected = await command.ExecuteNonQueryAsync();
                return $"✅ Команда выполнена успешно. Затронуто строк: {affected}";
            }
        }

        private static string EscapeCsv(object? value)
        {
            if (value == null) return "";
            var s = value.ToString()?.Replace("\"", "\"\"") ?? "";
            return $"\"{s}\"";
        }
    }
}
