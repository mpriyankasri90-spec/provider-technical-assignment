# SQLite database

| Path | Content |
| --- | --- |
| `Data/providers.db` | The application database with sample data (single file, rollback-journal mode) |
| `Scripts/01_create_tables.sql` | Tables, constraints and indexes |
| `Scripts/02_hard_delete_triggers.sql` | Triggers that abort any hard delete |
| `Scripts/03_reporting_views.sql` | Views: current data, the two license scenarios, deleted providers |
| `Scripts/04_seed_data.sql` | Sample data for every required scenario |
| `Scripts/05_scenario_queries.sql` | Scenario, audit and data-quality queries; soft-delete and restore templates |

Design decisions, soft-delete layers and license rules are documented in the [root README](../../../README.md#database-design).

## Where the schema comes from

EF Core migrations in `Data/Migrations` are the source of truth (views and triggers are part of them). Scripts 01–03 were generated from them:

```bash
dotnet ef migrations script 0 InitialSchema --project backend/Candidate.API
dotnet ef migrations script InitialSchema AddHardDeleteTriggers --project backend/Candidate.API
dotnet ef migrations script AddHardDeleteTriggers AddReportingViews --project backend/Candidate.API
```

The scripts also record the migrations in `__EFMigrationsHistory`, so a database built from them can still be upgraded by the application. Running 01 → 04 on an empty file produces the same schema, data and scenario results as `Data/providers.db`.

## Common tasks

- **Reset the sample data:** stop the API, delete `Data/providers.db`, and start the API in Development. It migrates and re-seeds automatically.
- **Build the database from SQL** with the `sqlite3` command-line tool:

  ```bash
  sqlite3 providers.db < Scripts/01_create_tables.sql
  sqlite3 providers.db < Scripts/02_hard_delete_triggers.sql
  sqlite3 providers.db < Scripts/03_reporting_views.sql
  sqlite3 providers.db < Scripts/04_seed_data.sql
  sqlite3 -header -column providers.db < Scripts/05_scenario_queries.sql
  ```

- **Inspect the data:** open `Data/providers.db` in DB Browser for SQLite or any SQLite client. Use `vw_Provider_Current` for standard results and `vw_Provider_Deleted` for audit.

Transient SQLite files (`*.db-wal`, `*.db-shm`, `*.db-journal`) are ignored by git. Commit the database only while the API is stopped.
