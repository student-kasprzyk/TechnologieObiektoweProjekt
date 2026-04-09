package crc64f8874d5b6cb775e9;


public class MapReadyCallback
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.gms.maps.OnMapReadyCallback
{

	public MapReadyCallback ()
	{
		super ();
		if (getClass () == MapReadyCallback.class) {
			mono.android.TypeManager.Activate ("GeoMapsPrototype.Platforms.Android.MapReadyCallback, GeoMapsPrototype", "", this, new java.lang.Object[] {  });
		}
	}

	public void onMapReady (com.google.android.gms.maps.GoogleMap p0)
	{
		n_onMapReady (p0);
	}

	private native void n_onMapReady (com.google.android.gms.maps.GoogleMap p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
