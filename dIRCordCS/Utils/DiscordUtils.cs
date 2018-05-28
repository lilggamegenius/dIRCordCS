using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DSharpPlus;
using DSharpPlus.Entities;

namespace dIRCordCS.Utils{
	public static class DiscordUtils{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetHostMask(this DiscordMember member){
			return $"{member.DisplayName}!{member.Username}@{member.Id}";
		}

		public static Permissions GetPermissions(this DiscordMember member){
			Permissions permissions = Permissions.None;
			foreach(DiscordRole role in member.Roles){
				permissions |= role.Permissions;
			}

			return permissions;
		}

		public static bool CanInteract(this DiscordMember issuer, DiscordMember target){
			if(issuer == null) throw new ArgumentNullException(nameof(issuer));
			if(target == null) throw new ArgumentNullException(nameof(target));

			DiscordGuild guild = issuer.Guild;
			if(guild != target.Guild) throw new ArgumentException("Provided members must both be Member objects of the same Guild!");
			if(guild.Owner == issuer) return true;
			if(guild.Owner == target) return false;
			DiscordRole issuerRole = issuer.Roles.FirstOrDefault();
			DiscordRole targetRole = target.Roles.FirstOrDefault();
			return issuerRole == null && (targetRole == null || CanInteract(issuerRole, targetRole));
		}

		public static bool CanInteract(this DiscordRole issuer, DiscordRole target){
			if(issuer == null) throw new ArgumentNullException(nameof(issuer));
			if(target == null) throw new ArgumentNullException(nameof(target));
			return target.Position < issuer.Position;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DiscordColor GetDiscordColor(this ColorMappings.Color color){
			return ColorMappings.colorDictionary[color].Item2;
		}
	}

	public class ColorMappings {
			public static readonly Dictionary<Color, (byte, DiscordColor)> colorDictionary = new Dictionary<Color, (byte, DiscordColor)>{
				{Color.Turquoise,		(10, new DiscordColor(26, 188, 156))},
				{Color.DarkTurquoise,	(10, new DiscordColor(17, 128, 106))},
				{Color.Green,			(9,	new DiscordColor(46, 204, 113))},
				{Color.DarkGreen,		(3,	new DiscordColor(31, 139, 76))},
				{Color.Blue,			(10, new DiscordColor(52, 152, 219))},
				{Color.DarkBlue,		(2, new DiscordColor(32, 102, 148))},
				{Color.Purple,			(13, new DiscordColor(155, 89, 182))},
				{Color.DarkPurple,		(6, new DiscordColor(113, 54, 138))},
				{Color.Pink,			(13, new DiscordColor(233, 30, 99))},
				{Color.DarkPink,		(6, new DiscordColor(173, 20, 87))},
				{Color.Yellow,			(8, new DiscordColor(241, 196, 15))},
				{Color.DarkYellow,		(8, new DiscordColor(194, 124, 14))},
				{Color.Orange,			(7, new DiscordColor(230, 126, 34))},
				{Color.DarkOrange,		(7, new DiscordColor(168, 67, 0))},
				{Color.Red,				(4, new DiscordColor(231, 76, 60))},
				{Color.DarkRed,			(5, new DiscordColor(153, 45, 34))},
				{Color.LightGray,		(0, new DiscordColor(149, 165, 166))},
				{Color.Gray,			(15, new DiscordColor(151, 156, 159))},
				{Color.DarkGray,		(14, new DiscordColor(96, 125, 139))},
				{Color.DarkerGray,		(1, new DiscordColor(84, 110, 122))}
			};

			public Color this[byte number]{
				get{
					foreach(Color color in colorDictionary.Keys){
						if(colorDictionary[color].Item1 == number)
							return color;
					}

					return 0;
				}
			}

			public Color this[DiscordColor findDiscordColor]{
				get{
					foreach(Color color in colorDictionary.Keys){
						if(colorDictionary[color].Item2.Equals(findDiscordColor))
							return color;
					}

					return 0;
				}
			}

			public enum Color : byte{
				Turquoise = 1,
				DarkTurquoise,
				Green,
				DarkGreen,
				Blue,
				DarkBlue,
				Purple,
				DarkPurple,
				Pink,
				DarkPink,
				Yellow,
				DarkYellow,
				Orange,
				DarkOrange,
				Red,
				DarkRed,
				LightGray,
				Gray,
				DarkGray,
				DarkerGray,
			}
		}
}
