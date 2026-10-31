-- Run on an isolated restored database BEFORE applying an additive migration.
-- Save the generated SQL privately; run that exact projection set before/after
-- each application. Output contains table names, row counts and MD5 consistency
-- fingerprints, not raw row values. Do not use this as a security hash or publish
-- private fingerprints. Migration history is excluded because it must change.
SELECT format(
    $query$SELECT %L || '|' || count(*) || '|' || md5(COALESCE(string_agg(payload, E'\n' ORDER BY payload COLLATE "C"), '')) FROM (SELECT row_to_json(original_row)::text AS payload FROM (SELECT %s FROM %I.%I) original_row) encoded_rows;$query$,
    c.table_name,
    string_agg(format('%I', c.column_name), ', ' ORDER BY c.ordinal_position),
    c.table_schema,
    c.table_name)
FROM information_schema.columns c
JOIN pg_tables t ON t.schemaname = c.table_schema AND t.tablename = c.table_name
WHERE c.table_schema = 'public' AND c.table_name <> '__EFMigrationsHistory'
GROUP BY c.table_schema, c.table_name
ORDER BY c.table_name COLLATE "C";
