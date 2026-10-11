# Booking and notification regression suite

Run `dotnet run --project ParkShare.Tests` from the repository root.
The executable exits nonzero on failure and uses the real SQL Server repositories.
Set `PARKSHARE_TEST_SQL` to a SQL Server connection string if the local SQLEXPRESS default is unavailable.
The runner overrides the catalog with a unique `ParkShare_Tests_<guid>` database, creates its schema, and deletes that database in `finally`. It never uses the application database.
The account needs permission to create and delete its test database.

The small executable runner uses existing project dependencies; no test framework or in-memory database is required.

The runner migrates the prior schema with a legacy notification before applying the additive notification migration. Notification cases cover recipient isolation, all booking events, repeated/concurrent decisions, read state, stable paging, uniqueness, and rollback when notification insertion fails.
