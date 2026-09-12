using System.Text;

namespace AmusementPark.Application.Features.Seo.Services;

internal sealed class StringWriterWithEncoding : StringWriter
{
    private readonly Encoding encoding;

    public StringWriterWithEncoding(Encoding encoding)
    {
        this.encoding = encoding;
    }

    public override Encoding Encoding => this.encoding;
}
