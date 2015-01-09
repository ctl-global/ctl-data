ctl-data
========

Ctl.Data provides parsers for CSV (including its variants like tab-delimited, pipe-delimited, etc.), fixed-width, and XLSX files.

As much as we prefer high-tech solutions like web services, technologies like CSV are one of the simplest formats to get started with and are deeply embedded in many industries.

Ctl.Data was designed to deal with real-world data files: ones which might be compiled by hand in Excel, or by inconsistent code. Beyond simply reading/writing these formats, it provides full diagnostics in the form of line and column numbers, serialization, and validation through .NET's standard facilities.

Robustness takes first priority, with a number of unit tests providing wide code coverage. A close second is performance. Hand-written parsers give wicked fast reading, and are I/O agnostic to provide both blocking and fully async implementations, fully native without just throwing stuff on a task pool. Serialization and validation skip slow reflection and use code generation to ensure these conveniences don't slow things down.
