using Unity.Netcode.Components;

public class ClientAuthTransform : NetworkTransform
{
  protected override bool OnIsServerAuthoritative() => false; // クライアント権限
}
