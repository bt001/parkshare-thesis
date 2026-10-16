# Booking, notification, chat, rating, amenity and photo regression suite

Run `dotnet run --project ParkShare.Tests` from the repository root.
The executable exits nonzero on failure and uses the real SQL Server repositories.
Set `PARKSHARE_TEST_SQL` to a SQL Server connection string if the local SQLEXPRESS default is unavailable.
The runner overrides the catalog with a unique `ParkShare_Tests_<guid>` database, creates its schema, and deletes that database in `finally`. It never uses the application database.
The account needs permission to create and delete its test database.

The small executable runner uses existing project dependencies; no test framework or in-memory database is required.

The runner migrates the prior schema with a legacy notification before applying the additive notification migration. Notification cases cover recipient isolation, all booking events, repeated/concurrent decisions, read state, stable paging, uniqueness, and rollback when notification insertion fails.

Chat cases cover owner/renter participation in every booking status and inactive listings, outsider isolation, text bounds and trimming, persistence across contexts, UTC timestamps, stable equal-time paging, large offsets and caller cancellation. Chat reuses the existing schema.

Completion and rating cases cover Romanian end-time boundaries, owner/renter isolation, inactive listings, repeated/concurrent completion, atomic completion notifications and rollback, serialized editable reviews, bounds, persistence, history, aggregates, stable pagination and listing-lock freshness. These flows reuse the existing schema and deterministic test clocks.

Amenity cases cover catalog seeding without replacing existing rows/associations, sorted detail reads, optional selection semantics, owner-only active/inactive edits, invalid/duplicate IDs, persistence, cancellation, atomic listing/role rollback, concurrent replacements and fresh ownership after the listing lock.

Photo cases cover upload bounds, ownership and cross-listing isolation, private storage key validation, persistence across context/storage recreation, cancellation, concurrent final-slot uploads, fresh ownership after lock waits, metadata/partial-file rollback, deletion-cleanup failures and lost commit acknowledgements. Each photo group uses and removes its own generated temporary directory. Browser coverage additionally verifies image rendering and persistence after an API process restart.
