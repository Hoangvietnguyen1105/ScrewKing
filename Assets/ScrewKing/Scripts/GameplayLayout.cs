using UnityEngine;

namespace ScrewKing
{
    public static class GameplayLayout
    {
        // GameUI prefab: layerMain stretches top=0, bottom=160; layerCen stretches
        // top=300, bottom=0. At 750x1334 its center is 667-300-(1334-160-300)/2=-70.
        // Keep the physics/oracle coordinate system intact; translate at its view boundary.
        public const float PhysicsCenterY=-230;
        public const float BoardCenterY=-70;
        public const float BoardOffsetPixels=BoardCenterY-PhysicsCenterY;
        public static Vector3 PhysicsToView(Vector3 point)=>point+Vector3.up*(BoardOffsetPixels/100f);
        public static Vector2 ViewToPhysics(Vector2 point)=>point-Vector2.up*(BoardOffsetPixels/100f);
    }
}
