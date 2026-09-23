using System.Runtime.CompilerServices;

// Allow the test project to access internal members (e.g. internal static builder methods
// on DataSet builders, used to unit-test business logic without a database).
[assembly: InternalsVisibleTo("Histo.Tests")]
