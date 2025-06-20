using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GodotNodeGenerator.Tests.TestHelpers
{
    /// <summary>
    /// Mock implementation of AdditionalText for testing
    /// </summary>
    internal class MockAdditionalText(string path, string content) : AdditionalText
    {
        private readonly string _path = path;
        private readonly string _content = content;

        public override string Path => _path;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_content, Encoding.UTF8);
        }
    }
}
