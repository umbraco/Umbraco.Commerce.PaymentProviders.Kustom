using System.Text;
using FluentAssertions;
using Umbraco.Commerce.PaymentProviders.Kustom.Api;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

namespace Umbraco.Commerce.PaymentProviders.Kustom.UnitTests
{
    public class KustomCallbackStreamReadTests
    {
        private const string SessionEventJson =
            "{\"event_id\":\"cd7e1171-25b1-41ff-97d3-b0dd5e6f9a82\",\"session\":{\"session_id\":\"39a1c773-bafd-754d-af1f-b30c592f1267\",\"status\":\"COMPLETED\",\"order_id\":\"a1a8f727-2756-6058-bd3c-40069be0994b\",\"kustom_reference\":\"X438HG0Q\",\"updated_at\":\"2019-05-13T14:54:04.675Z\",\"expires_at\":\"2019-05-15T13:51:43.507Z\",\"authorization_token\":\"a1a8f727-2756-6058-bd3c-40069be0994b\"}}";

        /// <summary>
        /// A stream that throws on synchronous reads, mimicking an ASP.NET Core
        /// request body on Kestrel/IIS with the default AllowSynchronousIO = false.
        /// </summary>
        private sealed class NoSyncReadStream : Stream
        {
            private readonly MemoryStream _inner = new(Encoding.UTF8.GetBytes(SessionEventJson));

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
                => throw new InvalidOperationException("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.");

            public override int ReadByte()
                => throw new InvalidOperationException("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.");

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => _inner.ReadAsync(buffer, offset, count, cancellationToken);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                => _inner.ReadAsync(buffer, cancellationToken);

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Flush()
            {
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private sealed class TestClientConfig : KustomClientConfig
        {
            public TestClientConfig()
                : base("username", "password", KustomApiRegion.Europe)
            {
            }

            public override string BaseUrl => "https://api.playground.kustom.co";
        }

        [Fact]
        public async Task ParseSessionEventAsync_NonSeekableNoSyncStream_Should_Succeed()
        {
            var client = new KustomClient(new TestClientConfig());
            using var stream = new NoSyncReadStream();

            var deserialized = await client.ParseSessionEventAsync(stream);

            deserialized.Should().NotBeNull();
            deserialized!.Session.Status.Should().Be("COMPLETED");
            deserialized.Session.OrderId.Should().Be("a1a8f727-2756-6058-bd3c-40069be0994b");
        }

        [Fact]
        public async Task ParseSessionEventAsync_SeekableStreamAlreadyRead_Should_Rewind()
        {
            var client = new KustomClient(new TestClientConfig());
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SessionEventJson));
            stream.Seek(0, SeekOrigin.End);

            var deserialized = await client.ParseSessionEventAsync(stream);

            deserialized.Should().NotBeNull();
            deserialized!.Session.Status.Should().Be("COMPLETED");
        }
    }
}
