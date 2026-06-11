using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum TestEnum {
	None,
	First,
	Second,
	Third,
	Final
}

public class Test : MonoScript {

	[SerializeField] float soundVolume = 1f;

	public override void Initialize() {
	}

	public override void Update() {

		AudioSource audioSource = entity.GetComponent<AudioSource>();
		if (audioSource != null) {
			audioSource.SetParams(soundVolume, 1f);
			audioSource.Play();
		}

	}
}


