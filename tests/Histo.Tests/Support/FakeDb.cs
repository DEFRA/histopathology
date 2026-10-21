using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Histo.Infrastructure;

namespace Histo.Tests.Support;

/// <summary>In-memory stand-in for the SQL layer, so Dapper-based repositories can be unit tested without a database.</summary>
public sealed class FakeDb : IDbConnectionFactory
{
    public List<ExecutedCommand> Commands { get; } = [];

    /// <summary>Result sets returned for a reader command; null means an empty result.</summary>
    public Func<ExecutedCommand, DataSet?> OnQuery { get; set; } = _ => null;

    /// <summary>Runs for every non-query command; may throw, or set output parameters via <see cref="ExecutedCommand.Command"/>.</summary>
    public Action<ExecutedCommand> OnExecute { get; set; } = _ => { };

    public object? ScalarResult { get; set; }
    public bool Committed { get; private set; }
    public bool RolledBack { get; private set; }

    public DbConnection CreateConnection() => new FakeConnection(this);

    public sealed class ExecutedCommand(FakeCommand command, Dictionary<string, object?> parameters)
    {
        public FakeCommand Command { get; } = command;
        public string Text => Command.CommandText;
        public Dictionary<string, object?> Parameters { get; } = parameters;
    }

    public sealed class FakeConnection(FakeDb db) : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public FakeDb Db { get; } = db;
        [AllowNull] public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "fake";
        public override string DataSource => "fake";
        public override string ServerVersion => "0";
        public override ConnectionState State => _state;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeTransaction(this);
        protected override DbCommand CreateDbCommand() => new FakeCommand(this);
        internal void MarkCommitted() => Db.Committed = true;
        internal void MarkRolledBack() => Db.RolledBack = true;
    }

    public sealed class FakeTransaction(FakeConnection connection) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        protected override DbConnection DbConnection => connection;
        public override void Commit() => connection.MarkCommitted();
        public override void Rollback() => connection.MarkRolledBack();
    }

    public sealed class FakeCommand : DbCommand
    {
        private readonly FakeConnection _connection;
        private readonly FakeParameterCollection _parameters = new();

        internal FakeCommand(FakeConnection connection) => _connection = connection;

        [AllowNull] public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeParameter();

        private ExecutedCommand Record()
        {
            var values = new Dictionary<string, object?>();
            foreach (FakeParameter p in _parameters) values[p.ParameterName] = p.Value;
            var executed = new ExecutedCommand(this, values);
            _connection.Db.Commands.Add(executed);
            return executed;
        }

        public override int ExecuteNonQuery()
        {
            _connection.Db.OnExecute(Record());
            return 1;
        }

        public override object? ExecuteScalar()
        {
            Record();
            return _connection.Db.ScalarResult;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var results = _connection.Db.OnQuery(Record());
            return results is { Tables.Count: > 0 } ? results.CreateDataReader() : new DataTable().CreateDataReader();
        }
    }

    public sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        [AllowNull] public override string ParameterName { get; set; } = string.Empty;
        public override int Size { get; set; }
        [AllowNull] public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var v in values) Add(v!); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }
}
