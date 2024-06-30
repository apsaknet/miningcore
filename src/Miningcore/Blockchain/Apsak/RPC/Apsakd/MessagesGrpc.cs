#pragma warning disable 0414, 1591, 8981, 0612
#region Designer generated code

namespace Miningcore.Blockchain.Apsak.Apsakd {

  using grpc = global::Grpc.Core;

  public partial class ApsakdP2P
  {
    public ApsakdP2P(string __ServiceName)
    {
      this.__Method_MessageStream = new grpc::Method<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage>(
        grpc::MethodType.DuplexStreaming,
        __ServiceName,
        "MessageStream",
        __Marshaller_protowire_ApsakdMessage,
        __Marshaller_protowire_ApsakdMessage);
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (message is global::Google.Protobuf.IBufferMessage)
      {
        context.SetPayloadLength(message.CalculateSize());
        global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
        context.Complete();
        return;
      }
      #endif
      context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static class __Helper_MessageCache<T>
    {
      public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (__Helper_MessageCache<T>.IsBufferMessage)
      {
        return parser.ParseFrom(context.PayloadAsReadOnlySequence());
      }
      #endif
      return parser.ParseFrom(context.PayloadAsNewBuffer());
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> __Marshaller_protowire_ApsakdMessage = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage.Parser));

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public grpc::Method<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> __Method_MessageStream { get; private set; }

    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Miningcore.Blockchain.Apsak.Apsakd.MessagesReflection.Descriptor.Services[0]; }
    }

    public partial class ApsakdP2PClient : grpc::ClientBase<ApsakdP2PClient>
    {
      public ApsakdP2P __ApsakdP2P { get; private set; }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public ApsakdP2PClient(ApsakdP2P __ApsakdP2P, grpc::ChannelBase channel) : base(channel)
      {
        this.__ApsakdP2P = __ApsakdP2P;
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public ApsakdP2PClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      protected ApsakdP2PClient() : base()
      {
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      protected ApsakdP2PClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual grpc::AsyncDuplexStreamingCall<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> MessageStream(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return MessageStream(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual grpc::AsyncDuplexStreamingCall<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> MessageStream(grpc::CallOptions options)
      {
        return CallInvoker.AsyncDuplexStreamingCall(__ApsakdP2P.__Method_MessageStream, null, options);
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      protected override ApsakdP2PClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new ApsakdP2PClient(configuration);
      }
    }

  }
  public partial class ApsakdRPC
  {
    public ApsakdRPC(string __ServiceName)
    {
      this.__Method_MessageStream = new grpc::Method<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage>(
        grpc::MethodType.DuplexStreaming,
        __ServiceName,
        "MessageStream",
        __Marshaller_protowire_ApsakdMessage,
        __Marshaller_protowire_ApsakdMessage);
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (message is global::Google.Protobuf.IBufferMessage)
      {
        context.SetPayloadLength(message.CalculateSize());
        global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
        context.Complete();
        return;
      }
      #endif
      context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static class __Helper_MessageCache<T>
    {
      public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (__Helper_MessageCache<T>.IsBufferMessage)
      {
        return parser.ParseFrom(context.PayloadAsReadOnlySequence());
      }
      #endif
      return parser.ParseFrom(context.PayloadAsNewBuffer());
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    static readonly grpc::Marshaller<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> __Marshaller_protowire_ApsakdMessage = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage.Parser));

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public grpc::Method<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> __Method_MessageStream { get; private set; }

    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Miningcore.Blockchain.Apsak.Apsakd.MessagesReflection.Descriptor.Services[1]; }
    }

    public partial class ApsakdRPCClient : grpc::ClientBase<ApsakdRPCClient>
    {
      public ApsakdRPC __ApsakdRPC { get; private set; }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public ApsakdRPCClient(ApsakdRPC __ApsakdRPC, grpc::ChannelBase channel) : base(channel)
      {
        this.__ApsakdRPC = __ApsakdRPC;
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public ApsakdRPCClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      protected ApsakdRPCClient() : base()
      {
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      protected ApsakdRPCClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual grpc::AsyncDuplexStreamingCall<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> MessageStream(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return MessageStream(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      public virtual grpc::AsyncDuplexStreamingCall<global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage, global::Miningcore.Blockchain.Apsak.Apsakd.ApsakdMessage> MessageStream(grpc::CallOptions options)
      {
        return CallInvoker.AsyncDuplexStreamingCall(__ApsakdRPC.__Method_MessageStream, null, options);
      }
      [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
      protected override ApsakdRPCClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new ApsakdRPCClient(configuration);
      }
    }

  }
}
#endregion
