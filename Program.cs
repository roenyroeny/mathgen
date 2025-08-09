using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace mathgen
{
	public static class Util
	{
		public static readonly char[] Letters = { 'x', 'y', 'z', 'w', 'i', 'j', 'k', 'l' };
	}
	public class Scalar
	{
		public enum Type
		{
			Int,
			UInt,
			Float,
		}

		public Type type;
		public int bitness;

		public string stdtype
		{
			get
			{
				switch (type)
				{
					case Type.Int:
						return $"int{bitness}_t";
					case Type.UInt:
						return $"uint{bitness}_t";
					case Type.Float:
						if (bitness == 32)
							return $"float";
						if (bitness == 64)
							return $"double";
						break;
				}
				return "UNKNOWN";
			}
		}

		public string Name
		{
			get
			{
				switch (type)
				{
					case Type.Int:
						return $"i{bitness}";
					case Type.UInt:
						return $"u{bitness}";
					case Type.Float:
						return $"f{bitness}";
				}
				return "UNKNOWN";
			}
		}

		public string Declaration
		{
			get
			{
				return $"export using {Name} = {stdtype};\n";
			}
		}
	};

	public class Vector
	{
		public Scalar type;
		public int width;
		public string Name
		{
			get
			{
				return $"{type.Name}_{width}";
			}
		}

		public string Ctors
		{
			get
			{
				string str = "";
				str += $"\t{Name}(){{}}";
				str += $"\t{Name}(";
				for (int i = 0; i < width; i++)
				{
					str += $"{type.Name} _{Util.Letters[i]}";
					str += (i != width - 1) ? ", " : "";
				}
				str += ") : ";
				for (int i = 0; i < width; i++)
				{
					str += $"{Util.Letters[i]}(_{Util.Letters[i]})";
					str += (i != width - 1) ? ", " : "";
				}
				str += "{}\n";
				return str;
			}
		}
		public string Operator1(string op)
		{
			string str = "";
			str += $"\tfriend {Name} operator {op} (const {Name}& a)";
			str += $" {{ return {Name}(";
			for (int i = 0; i < width; i++)
			{
				str += $"{op}a.{Util.Letters[i]}";
				str += (i != width - 1) ? ", " : "";
			}
			str += "); }";
			str += "\n";
			return str;
		}
		public string Operator2s(string op)
		{
			string str = "";
			str += $"\tfriend {Name} operator {op} (const {Name}& a, {type.Name} b)";
			str += $" {{ return {Name}(";
			for (int i = 0; i < width; i++)
			{
				str += $"a.{Util.Letters[i]} {op} b";
				str += (i != width - 1) ? ", " : "";
			}
			str += "); }";
			str += "\n";
			return str;
		}

		public string Operator2(string op)
		{
			string str = "";
			str += $"\tfriend {Name} operator {op} (const {Name}& a, const {Name}& b)";
			str += $" {{ return {Name}(";
			for (int i = 0; i < width; i++)
			{
				str += $"a.{Util.Letters[i]} {op} b.{Util.Letters[i]}";
				str += (i != width - 1) ? ", " : "";
			}
			str += "); }";
			str += "\n";
			return str;
		}
		public string PerComp(string name, string func)
		{
			string str = "";
			str += $"\tfriend {Name} {name}(const {Name}& a, const {Name}& b)";
			str += $" {{ return {Name}(";
			for (int i = 0; i < width; i++)
			{
				str += $"{func}(a.{Util.Letters[i]}, b.{Util.Letters[i]})";
				str += (i != width - 1) ? ", " : "";
			}
			str += "); }";
			str += "\n";
			return str;
		}
		public string PerComp(string name) { return PerComp(name, name); }
		public string Functions
		{
			get
			{
				string str = "";
				if (type.type == Scalar.Type.Float)
					str += FloatFunctions();

				str += PerComp("min", "std::min");
				str += PerComp("max", "std::max");

				return str;
			}
		}
		public string Operators
		{
			get
			{
				string str = "";
				if (type.type != Scalar.Type.UInt)
					str += Operator1("-");
				str += Operator2s("+");
				str += Operator2("+");
				str += Operator2s("-");
				str += Operator2("-");
				str += Operator2s("*");
				str += Operator2("*");
				str += Operator2s("/");
				str += Operator2("/");
				str += IndexOperator();
				return str;
			}
		}
		public string IndexOperator()
		{
			string str = "";
			str += $"\t{type.Name} operator [] (i32 i) const {{ return c[i]; }}\n";
			str += $"\t{type.Name}& operator [] (i32 i) {{ return c[i]; }}\n";
			return str;
		}
		public string MinFunction()
		{
			string str = "";
			return str;
		}
		public string FloatFunctions()
		{
			string str = "";
			str += DotFunction();
			if (type.type == Scalar.Type.Float)
				str += LengthFunction();

			if (width == 3)
				str += CrossFunction();

			return str;
		}

		public string DotFunction()
		{
			string str = "";
			str += $"\tfriend {type.Name} dot(const {Name}& a, const {Name}& b) {{ ";
			str += $"return ";
			for (int i = 0; i < width; i++)
			{
				str += $"a.{Util.Letters[i]} * b.{Util.Letters[i]}";
				str += (i != width - 1) ? " + " : "";
			}
			str += "; }\n";
			return str;
		}

		public string LengthFunction()
		{
			string str = "";
			str += $"\tfriend {type.Name} length(const {Name}& a) {{ ";
			str += $"return ({type.Name})sqrt(dot(a, a)); }}\n";
			return str;
		}
		public string CrossFunction()
		{
			string str = "";
			str += $"\tfriend {Name} cross(const {Name}& a, const {Name}& b) {{ ";
			str += $"return {Name}(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);}}";
			return str;
		}


		public string Declaration
		{
			get
			{
				string str = $"export struct {Name}\n{{\n";
				str += "\tunion\n\t{\n";
				str += $"\t\t{type.Name} c[{width}];\n";
				str += "\t\tstruct\n\t\t{\n";

				str += $"\t\t\t{type.Name} ";
				for (int i = 0; i < width; i++)
				{
					str += $"{Util.Letters[i]}";
					str += (i != width - 1) ? ", " : ";\n";
				}
				str += "\t\t};\n";
				str += "\t};\n";

				str += Ctors;
				str += Operators;
				str += Functions;

				str += "};\n";


				return str;
			}
		}
	};

	public class Matrix
	{
		public Vector type;
		public int height;
		public string Name
		{
			get
			{
				return $"{type.Name}_{height}";
			}
		}
	};

	public class Program
	{
		public static void Main(string[] args)
		{
			List<Scalar> scalars = new List<Scalar>();
			for (int i = 0; i < 4; i++)
			{
				int[] bitness = { 8, 16, 32, 64 };
				scalars.Add(new Scalar { type = Scalar.Type.Int, bitness = bitness[i] });
				scalars.Add(new Scalar { type = Scalar.Type.UInt, bitness = bitness[i] });
				if (i > 1)
					scalars.Add(new Scalar { type = Scalar.Type.Float, bitness = bitness[i] });
			}
			List<Vector> vectors = new List<Vector>();
			foreach (var s in scalars)
			{
				vectors.Add(new Vector { type = s, width = 2 });
				vectors.Add(new Vector { type = s, width = 3 });
				vectors.Add(new Vector { type = s, width = 4 });
				vectors.Add(new Vector { type = s, width = 8 });
			}

			string code = "";
			code += "module;\n";
			code += "#include <stdint.h>\n";
			code += "#include <algorithm>\n";
			code += "export module types;\n";

			foreach (var s in scalars)
				code += s.Declaration;

			foreach (var v in vectors)
				code += v.Declaration;

			System.IO.File.WriteAllText("types.ixx", code);
		}
	};
}