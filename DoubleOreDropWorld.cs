using DoubleOreDrop.Netcode.Packets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DoubleOreDrop
{
	public class DoubleOreDropWorld : ModWorld
	{
		//HashSet because .Contains checks are incredibly fast with it
		public static HashSet<Point16> placedSpots;

		public override void Initialize()
		{
			placedSpots = new HashSet<Point16>();
		}

		public override TagCompound Save()
		{
			//Because tml TagCompound doesn't support HashSet<Point16>, we have to do some conversions here to List<Point16>
			int count = placedSpots.Count;
			if (count == 0) return null;

			Point16[] Point16Array = new Point16[count];
			placedSpots.CopyTo(Point16Array);

			List<Point16> Point16List = Point16Array.ToList();

			return new TagCompound
			{
				{"placedSpots", Point16List}
			};
		}

		public override void Load(TagCompound tag)
		{
			//Because tml TagCompound doesn't support HashSet<Point16>, we have to do some conversions here from List<Point16>
			var Point16IList = tag.GetList<Point16>("placedSpots");
			placedSpots = new HashSet<Point16>(Point16IList);
		}

		public override void NetReceive(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Point16[] Point16Array = new Point16[count];

			for (int i = 0; i < count; i++)
			{
				short x = reader.ReadInt16();
				short y = reader.ReadInt16();
				Point16Array[i] = new Point16(x, y);
			}

			placedSpots = new HashSet<Point16>(Point16Array);
		}

		public override void NetSend(BinaryWriter writer)
		{
			int count = placedSpots.Count;
			Point16[] Point16Array = new Point16[count];
			placedSpots.CopyTo(Point16Array);

			writer.Write((int)count);

			for (int i = 0; i < count; i++)
			{
				Point16 spot = Point16Array[i];
				writer.Write(spot.X);
				writer.Write(spot.Y);
			}
		}

		#region Methods
		public static void TryAddSpot(Point16 spot, bool clientWantsBroadcast = false)
		{
			if (!placedSpots.Contains(spot))
			{
				//Don't allow duplicates to be added
				placedSpots.Add(spot);
			}

			if (Main.netMode == NetmodeID.MultiplayerClient && clientWantsBroadcast)
			{
				new AddSpotPacket(spot).Send();
			}
		}

		public static void RemoveSpot(Point16 spot)
		{
			//Initial call is serverside
			placedSpots.Remove(spot);

			if (Main.netMode == NetmodeID.Server)
			{
				new RemoveSpotPacket(spot).Send();
			}
		}
		#endregion
	}
}