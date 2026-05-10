# SQL Visualizer

An interactive SQL visualization tool designed to help students understand how SQL queries execute step-by-step.

Built as part of a bachelor thesis at the IT University of Copenhagen.


## Overview

Learning SQL can be difficult because query execution is abstract and often invisible to students. Operations such as:

- `JOIN`
- `GROUP BY`
- `HAVING`
- Window Functions

require learners to mentally simulate how data changes throughout execution.

SQL Visualizer makes these transformations visible through interactive animations and step-by-step visualizations of SQL execution.

The project was developed to support stronger mental models of SQL queries and improve conceptual understanding of complex database operations.

# Why This Project Exists

Research and teaching experience show that students often struggle with SQL not because of syntax, but because they lack a clear understanding of how queries execute internally.

Traditional teaching methods usually show only:
- Input tables
- Final query results

without visualizing the intermediate transformations.

This project aims to bridge that gap through:
- Visualization
- Animation
- Step-by-step execution
- Interactive exploration

# Thesis & Research

This project was developed as part of the bachelor thesis:

**Didactic Query Visualization: Learning SQL Through Interactive Animation and Visualization**

The thesis explored whether interactive SQL visualizations could improve:
- SQL learning
- Perceived understanding
- Mental models of SQL execution

Experiment results indicated:
- Positive student reception
- Increased perceived understanding
- Strong interest in interactive SQL learning tools

# Tech Stack

- ASP.NET Core
- Blazor
- DuckDB
- Docker

# Running the Project

## Prerequisites

Install:

- [.NET SDK](https://dotnet.microsoft.com/)
- Docker (optional)


## Clone the repository

```bash
git clone https://github.com/stoftot/Itu-bsc-2026-sqlVisualizer.git
```


## Run locally

```bash
cd Itu-bsc-2026-sqlVisualizer/sqlVisualizer/visualizer
dotnet run
```

Then open:

```txt
http://localhost:8080
```


# Running with Docker

## Build image

```bash
cd Itu-bsc-2026-sqlVisualizer/sqlVisualizer
docker build -t sql-visualizer .
```

## Run container

```bash
docker run -p 8080:8080 sql-visualizer
```

# Contributing

Contributions are welcome.

If you want to contribute through code, documentation, bug reports, or educational ideas, please read the contribution guide:

[CONTRIBUTING.md](CONTRIBUTING.md)

Examples of helpful contributions:
- Bug fixes
- UI improvements
- New SQL visualizations
- Documentation
- Example datasets
- Educational material
