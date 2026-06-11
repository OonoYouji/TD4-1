using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class AudioSource : Component {
	///////////////////////////////////////////////////////////////////////////////////////////
	/// objects
	///////////////////////////////////////////////////////////////////////////////////////////
	public float volume = 1f;
	public float pitch = 1f;
	public string path = "";
	private bool isPlayRequest_;

	///////////////////////////////////////////////////////////////////////////////////////////
	/// method
	///////////////////////////////////////////////////////////////////////////////////////////

	public void Play() {
		Debug.Log($"[AudioSource.Play] Path: {path}, Vol: {volume}, Pitch: {pitch}");
		isPlayRequest_ = true;
		InternalSetParams(nativeHandle, volume, pitch);
		InternalPlay(nativeHandle);
	}

	public void Stop() {
		Debug.Log($"[AudioSource.Stop] Requested for entity ID: {nativeHandle}");
		InternalStop(nativeHandle);
	}

	public void OneShotPlay(float _volume, float _pitch, string _path) {
		Debug.Log($"[AudioSource.OneShotPlay] Path: {_path}, Vol: {_volume}, Pitch: {_pitch}");
		InternalPlayOneShot(nativeHandle, _volume, _pitch, _path);
	}

	///////////////////////////////////////////////////////////////////////////////////////////
	/// internal methods
	///////////////////////////////////////////////////////////////////////////////////////////
	[MethodImpl(MethodImplOptions.InternalCall)]
	static private extern void InternalGetParams(ulong _nativeHandle, out float _volume, out float _pitch);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static private extern void InternalSetParams(ulong _nativeHandle, float volume, float pitch);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static private extern void InternalPlay(ulong _nativeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static private extern void InternalStop(ulong _nativeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static private extern void InternalPlayOneShot(ulong _nativeHandle, float _volume, float _pitch, string _path);
}
