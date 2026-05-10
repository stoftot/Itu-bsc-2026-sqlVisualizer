# System Pipeline

This document explains the SQL Visualizer at a system level: how a query moves through the application, what each stage is responsible for, and how the result becomes an interactive animation in the UI.

## Overview

The application takes a SQL `SELECT` query and turns it into a step-by-step visual explanation of how the query is executed.

The main pipeline is:

1. The user selects a database and writes SQL.
2. The query is validated.
3. The SQL is decomposed into execution-relevant clauses.
4. Intermediate tables are generated for each clause.
5. Those intermediate tables are converted into animation steps.
6. The UI renders the current execution step and lets the user play, pause, or step through the animation.

## 1. Input and database selection

The Blazor UI is centered around three pieces:

- `SchemaView`: lets the user select the active database and inspect available tables
- `EditorView`: hosts the SQL editor
- `ToolBar`: runs queries and controls the currently loaded visualization

The currently selected database is stored through a scoped database context. When the user switches database, the active connection string is updated and the schema sidebar is reloaded.

The editor content is also stored per session and per database, so changing database can restore a different query for that database.

## 2. Query validation

Before visualization, the query is validated in two ways:

- syntax validation through DuckDB parsing
- execution validation against the currently selected database

This matters because the rest of the pipeline assumes the query can both be parsed and executed. If validation fails, the pipeline stops early and the UI shows an error popup instead of trying to generate steps from invalid SQL.

## 3. SQL decomposition

After validation, the SQL is decomposed into top-level clauses such as:

- `WITH`
- `SELECT`
- `FROM`
- `JOIN`
- `WHERE`
- `GROUP BY`
- `HAVING`
- `ORDER BY`
- `LIMIT`

This stage is not trying to build a full SQL AST. Its job is narrower:

- identify the major clauses relevant to the visualization
- preserve the clause text
- order the clauses according to execution semantics rather than SQL writing order

There are two important preprocessing ideas here:

- select aliases are expanded where needed, so later stages do not have to reason about alias references in `WHERE`, `GROUP BY`, `HAVING`, and join conditions
- clause detection is depth-aware, so keywords inside strings, comments, subqueries, and CTE bodies do not accidentally split the outer query

The output of this stage is a sequence of SQL components that the table-generation pipeline can execute one step at a time.

## 4. How tables are generated

This is the core of the system.

The application does not just execute the full query once. Instead, it incrementally builds the query one clause at a time and captures what the tables look like before and after each step.

For each step, the system creates:

- `from tables`: the input tables shown on the left side of the visualization
- `to tables`: the output tables shown on the right side of the visualization

### Initial table state

The first executable state is built from the earliest relevant clause, usually `FROM` or `WITH + FROM`.

At this point, the system loads the source table data and also tracks where each output column originally came from. That origin tracking is important later for:

- qualified columns like `table.column`
- wildcard selection like `table.*`
- join visualizations
- highlighting the correct source columns during `SELECT`

### Step-by-step transformation

Each additional clause is appended to the current query state and executed to produce the next output table state.

Different clause types are handled differently:

- `JOIN`:
  the generator builds the joining input tables explicitly and then matches them against the executed result
- `WHERE`:
  rows are filtered from one table into another
- `GROUP BY`:
  one input table becomes multiple grouped output tables
- `HAVING`:
  grouped tables are filtered based on aggregate results
- `SELECT`:
  columns and aggregates are projected into the final visible shape
- `ORDER BY`:
  row order changes
- `LIMIT`:
  rows are truncated

The important design choice is that the generator always relies on the database system for the actual query result, but builds extra structure around that result so the system can explain how the transformation happened.

In other words:

- Database system decides what the correct result is
  - With the exception of `GROUP BY`
- The table-generation layer decides how to represent the transition into that result

## 5. How animation is generated

Once the pipeline has intermediate execution steps, those steps are converted into animations.

Each SQL step becomes one animation object with:

- the clause keyword it represents
- the `from tables`
- the `to tables`
- a list of visual actions that can be replayed one by one

Those visual actions are small mutations such as:

- show or hide a cell
- highlight a row
- highlight a set of columns
- highlight an aggregate
- change highlight color
- reset a table back to its neutral state

### Animation style by clause

Each clause type gets its own animation strategy:

- `JOIN`: highlights candidate rows across the source and joining tables, then reveals matching output rows
- `WHERE`: highlights rows that satisfy the predicate and shows which rows remain
- `GROUP BY`: highlights grouping columns and gradually fills grouped output tables
- `HAVING`: highlights group aggregates and shows which groups survive the filter
- `SELECT`: highlights source columns or aggregate inputs and reveals projected columns in the result
- `ORDER BY`: re-inserts rows into the result in sorted order
- `LIMIT`: reveals only the retained rows

This means the animation layer is not about SQL correctness by itself. It is about expressing the already computed step transition in a way that is understandable to the user.

## 6. How display works

The animation layer works on display-oriented table models rather than raw execution tables.

These display models contain:

- column names
- visible rows
- visible cells
- highlight state
- inline style information
- aggregate display state

The UI does not recalculate the visualization logic itself. Instead, it renders whatever state the current animation object exposes.

The main display flow is:

1. `QueryIllustrationView` loads the generated animations.
2. The first SQL step is selected.
3. The current animation exposes its `from` and `to` display tables.
4. `TableView` renders those tables.
5. When the user steps forward or presses play, the next visual action mutates the display tables.
6. Blazor re-renders the updated state.

Because the display tables are mutable, replay works by resetting the current animation and then replaying actions up to the requested point.

## 7. UI control flow

`HomeState` is the coordination object that keeps the page synchronized.

It stores:

- the editor reference
- the loaded animation steps
- the current SQL step index
- the current animation sub-step index
- whether animation is playing
- the current editor query
- the last visualized query
- the selected database

The toolbar does not know how to generate animations directly. Instead, it calls delegates stored in `HomeState`, and those delegates are assigned by `QueryIllustrationView`.

That design makes `HomeState` the bridge between:

- input controls
- animation playback
- page-level state

## 8. Metrics and persistence

Two supporting subsystems run alongside the main pipeline.

### User persistence

The user repository stores:

- the current query per session and database
- the list of uploaded user databases

This makes the editor feel stateful even though the active data source can change.

### Metrics

The metrics subsystem records:

- query executions
- button/action counts
- time spent on each SQL step
- time spent actively playing animations
- actions grouped by SQL keyword
- average percentage of each animation viewed

The `/metrics` page reads those aggregates and displays them as charts.

## 9. Design intent

The solution is best understood as three stacked layers:

- query understanding:
  validate SQL and decompose it into meaningful clauses
- execution explanation:
  generate intermediate tables that describe the transformation
- visual playback:
  turn those transformations into interactive animations in the UI

That separation is the main idea behind the system. The application is not just a SQL runner and not just a front-end animation tool. It is a translation pipeline from executable SQL into explainable visual state transitions.

## 10. Current limits

A few limits are worth keeping in mind when working on the system:

- the pipeline is focused on visualizing `SELECT`-style queries
- `OFFSET` exists in the model but is not fully visualized
- the animation layer depends on mutable display objects and replay/reset behavior rather than immutable snapshots

If you are changing behavior, the most important question is usually:

"Is this change about parsing, execution-step generation, animation generation, or UI playback?"

That question tells you which part of the pipeline should own the change.
