﻿using UnityEngine;
using System.Collections;

public class SMBBlockBreakable : SMBBlock {

	override protected void DestroyBlock (SMBPlayer player) {

		if (player.State == SMBConstants.PlayerState.GrownUp) {

			SMBGameWorld.Instance.PlayParticle (transform.position, "SMBBlockParticleSystem");
			SMBGameWorld.Instance.PlaySoundEffect ((int)SMBConstants.GameWorldSoundEffects.BreakBlock);

            //for Mario's OnTriggerStay
            Interaction = "Break";
            Action = "Jump";
            updateBeliefArray();
            player.printArray(beliefArray);

			Destroy (gameObject);
		}
	}
}
