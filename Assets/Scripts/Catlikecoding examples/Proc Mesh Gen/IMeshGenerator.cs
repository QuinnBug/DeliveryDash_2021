using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralMeshes
{
	public interface IMeshGenerator
	{
		int Resolution { get; set; }

		void Execute<S>(int i, S streams) where S : struct, IMeshStreams;

		int VertexCount { get; }

		int IndexCount { get; }

		int JobLength { get; }

		Bounds Bounds { get; }
	}
}
