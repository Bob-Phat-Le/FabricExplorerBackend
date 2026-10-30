using FabricExplorerBackend.Application.QueryService;
using Parquet.Data;
using Parquet.Schema;
using System.Data.Common;
using System.Globalization;

namespace FabricExplorerBackend.Infrastructures.QueryResults
{
    /// <summary>
    /// Typed, reusable buffer for one column of the current row group. Rows are appended straight from the
    /// <see cref="DbDataReader"/>; <see cref="Flush"/> hands the buffered values to the Parquet writer.
    /// </summary>
    internal abstract class ParquetColumn
    {
        protected ParquetColumn(DataField field, QueryResultColumn descriptor)
        {
            Field = field;
            Descriptor = descriptor;
        }

        public DataField Field { get; }
        public QueryResultColumn Descriptor { get; }

        public abstract void Append(DbDataReader reader, int ordinal);

        /// <summary>Returns the buffered values and resets the buffer. Write the column before appending again.</summary>
        public abstract DataColumn Flush();
    }

    internal sealed class ParquetColumn<T>(
        DataField field,
        QueryResultColumn descriptor,
        int capacity,
        Func<DbDataReader, int, T> read) : ParquetColumn(field, descriptor)
    {
        private readonly T[] values = new T[capacity];
        private int count;

        public override void Append(DbDataReader reader, int ordinal)
            => values[count++] = reader.IsDBNull(ordinal) ? default! : read(reader, ordinal);

        public override DataColumn Flush()
        {
            var data = count == values.Length ? values : values.AsSpan(0, count).ToArray();
            count = 0;
            return new DataColumn(Field, data);
        }
    }

    internal static class ParquetColumnFactory
    {
        // Largest decimal the default Parquet decimal encoding (precision 38, scale 18) and System.Decimal both hold.
        private const int MaxDecimalPrecision = 28;
        private const int MaxDecimalScale = 18;
        private const int MaxDecimalIntegerDigits = 20;

        public static ParquetColumn Create(DbColumn source, string name, int capacity)
        {
            var clr = source.DataType;
            var sqlType = source.DataTypeName ?? clr?.Name ?? "unknown";
            var lower = sqlType.ToLowerInvariant();

            // Types whose CLR type alone is not enough.
            switch (lower)
            {
                case "date":
                    return Make(new DataField<DateTime?>(name), "date", sqlType, capacity,
                        (r, i) => (DateTime?)DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc));
                case "money":
                case "smallmoney":
                    return Make(new DataField<decimal?>(name), "decimal", sqlType, capacity, (r, i) => (decimal?)r.GetDecimal(i));
                case "time":
                case "datetimeoffset":
                    return AsString(name, sqlType, capacity);
            }

            if (clr == typeof(bool))
                return Make(new DataField<bool?>(name), "boolean", sqlType, capacity, (r, i) => (bool?)r.GetBoolean(i));
            if (clr == typeof(byte))
                return Make(new DataField<byte?>(name), "byte", sqlType, capacity, (r, i) => (byte?)r.GetByte(i));
            if (clr == typeof(short))
                return Make(new DataField<short?>(name), "int16", sqlType, capacity, (r, i) => (short?)r.GetInt16(i));
            if (clr == typeof(int))
                return Make(new DataField<int?>(name), "int32", sqlType, capacity, (r, i) => (int?)r.GetInt32(i));
            if (clr == typeof(long))
                return Make(new DataField<long?>(name), "int64", sqlType, capacity, (r, i) => (long?)r.GetInt64(i));
            if (clr == typeof(float))
                return Make(new DataField<float?>(name), "float", sqlType, capacity, (r, i) => (float?)r.GetFloat(i));
            if (clr == typeof(double))
                return Make(new DataField<double?>(name), "double", sqlType, capacity, (r, i) => (double?)r.GetDouble(i));
            if (clr == typeof(Guid))
                return Make(new DataField<Guid?>(name), "guid", sqlType, capacity, (r, i) => (Guid?)r.GetGuid(i));
            if (clr == typeof(byte[]))
                return Make(new DataField<byte[]>(name), "binary", sqlType, capacity, (r, i) => r.GetFieldValue<byte[]>(i));

            if (clr == typeof(DateTime))
            {
                // Mark as UTC so the library stores the wall-clock value untouched (SQL datetime has no time zone).
                return Make(new DataField<DateTime?>(name), "datetime", sqlType, capacity,
                    (r, i) => (DateTime?)DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc));
            }

            if (clr == typeof(decimal))
            {
                var precision = source.NumericPrecision ?? 0;
                var scale = source.NumericScale ?? 0;
                if (precision is >= 1 and <= MaxDecimalPrecision
                    && scale is >= 0 and <= MaxDecimalScale
                    && precision - scale <= MaxDecimalIntegerDigits)
                {
                    return Make(new DataField<decimal?>(name), "decimal", sqlType, capacity, (r, i) => (decimal?)r.GetDecimal(i));
                }

                // decimal(38, x) can exceed System.Decimal: keep every digit as text.
                return Make(new DataField<string>(name), "string", sqlType, capacity,
                    (r, i) => r.GetProviderSpecificValue(i)?.ToString());
            }

            return AsString(name, sqlType, capacity);
        }

        private static ParquetColumn AsString(string name, string sqlType, int capacity)
            => Make(new DataField<string>(name), "string", sqlType, capacity, (r, i) => ToText(r.GetValue(i)));

        private static ParquetColumn Make<T>(DataField field, string dataType, string sqlType, int capacity, Func<DbDataReader, int, T> read)
            => new ParquetColumn<T>(field, new QueryResultColumn(field.Name, sqlType, dataType), capacity, read);

        private static string? ToText(object? value) => value switch
        {
            null => null,
            string s => s,
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToHexString(bytes),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
