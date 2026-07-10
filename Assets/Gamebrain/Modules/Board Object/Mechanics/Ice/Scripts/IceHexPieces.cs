using UnityEngine;

namespace Gameplay
{
    public class IceHexPieces : MonoBehaviour
    {
        public GameObject[] IcePieces;
        public Transform IceBrokenPiecesContainer;

        public void SetState(int totalEnabled)
        {
            for (int i = 0; i < IcePieces.Length; i++)
            {
                IcePieces[i].SetActive(i == totalEnabled - 1);
            }
        }

        public void PlayBrokenPieceAnimation(int cost)
        {
            // BrokenIceHex.Create(cost, IceBrokenPiecesContainer.position);
        }
    }
}
