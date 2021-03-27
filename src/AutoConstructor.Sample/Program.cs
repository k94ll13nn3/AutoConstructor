using System;

namespace AutoConstructor.Sample
{
    internal static class Program
    {
        private static void Main()
        {
            var test = new Test2("test", DateTime.Now, Guid.NewGuid(), 89, "valm", new Data { Key = "key" });
            test.Dump();
        }
    }

    [AutoConstructor]
    internal partial class Test2
    {
        private readonly string _name;
        private readonly Uri _uri = new("/non-modified", UriKind.Relative);

        [AutoConstructorIgnore]
        private readonly DateTime _date;

        private readonly DateTime? _date2;

        [AutoConstructorInject("guid.ToString()", "guid", typeof(Guid))]
        private readonly string _guidString;

        [AutoConstructorInject("guid.ToString().Length", "guid", typeof(Guid))]
        private readonly int _guidLength;

        [AutoConstructorInject("name.ToUpper()", "name", typeof(string))]
        private readonly string _nameShared;

        private readonly int _number;

        [AutoConstructorInject("val.Length", "val", typeof(string))]
        private readonly int _length;

        private readonly Data _data;

        public void Dump()
        {
            Console.WriteLine(_name);
            Console.WriteLine(_uri);
            Console.WriteLine(_date);
            Console.WriteLine(_date2);
            Console.WriteLine(_guidString);
            Console.WriteLine(_guidLength);
            Console.WriteLine(_nameShared);
            Console.WriteLine(_number);
            Console.WriteLine(_length);
            Console.WriteLine(_data.Key);
        }
    }

    internal class Data
    {
        public string Key { get; set; } = "";
    }
}
