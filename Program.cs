using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace mathgen
{
	public static class Util
	{
		public static readonly char[] Components = { 'x', 'y', 'z', 'w', 'i', 'j', 'k', 'l' };
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
				str += $"\t{Name}() = default;\n";
				str += $"\t{Name}(";
				for (int i = 0; i < width; i++)
				{
					str += $"{type.Name} _{Util.Components[i]}";
					str += (i != width - 1) ? ", " : "";
				}
				str += ") : ";
				for (int i = 0; i < width; i++)
				{
					str += $"{Util.Components[i]}(_{Util.Components[i]})";
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
				str += $"{op}a.{Util.Components[i]}";
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
				str += $"a.{Util.Components[i]} {op} b";
				str += (i != width - 1) ? ", " : "";
			}
			str += "); }";
			str += "\n";
			str += $"\t{Name} operator {op}= (const {type.Name}& o) {{ *this = *this {op} o; return *this; }}\n";
			return str;
		}
		public string Operator2(string op)
		{
			string str = "";
			str += $"\tfriend {Name} operator {op} (const {Name}& a, const {Name}& b)";
			str += $" {{ return {Name}(";
			for (int i = 0; i < width; i++)
			{
				str += $"a.{Util.Components[i]} {op} b.{Util.Components[i]}";
				str += (i != width - 1) ? ", " : "";
			}
			str += "); }";
			str += "\n";
			str += $"\t{Name} operator {op}= (const {Name}& o) {{ *this = *this {op} o; return *this; }}\n";
			return str;
		}
		public string PerComp(string name, string func)
		{
			string str = "";
			str += $"\tfriend {Name} {name}(const {Name}& a, const {Name}& b)";
			str += $" {{ return {Name}(";
			for (int i = 0; i < width; i++)
			{
				str += $"{func}(a.{Util.Components[i]}, b.{Util.Components[i]})";
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
				str += $"a.{Util.Components[i]} * b.{Util.Components[i]}";
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
				str += $"\t\tstruct {{ {type.Name} ";
				for (int i = 0; i < width; i++)
				{
					str += $"{Util.Components[i]}";
					str += (i != width - 1) ? ", " : "";
				}
				str += "; };\n";

				// union smaller vectors in there as well. but only for small ish vectors
				if (width <= 4)
					for (int i = 2; i < width; i++)
					{
						str += $"\t\t{type.Name}_{i} ";
						for (int j = 0; j < i; j++)
							str += Util.Components[j];
						str += ";\n";
					}

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
				return $"{type.Name}x{height}";
			}
		}

		public int width => type.width;

		public string ColName
		{
			get
			{
				return $"{type.type.Name}_{height}";
			}
		}

		public string Ctors
		{
			get
			{
				string str = "";
				str += $"\t{Name}() = default;\n";

				str += $"\texplicit {Name}(";
				for (int i = 0; i < height; i++)
				{
					str += $"{type.Name} _{Util.Components[i]}";
					str += (i != height - 1) ? ", " : "";
				}
				str += ") : ";
				for (int i = 0; i < height; i++)
				{
					str += $"{Util.Components[i]}(_{Util.Components[i]})";
					str += (i != height - 1) ? ", " : "";
				}
				str += "{}\n";

				str += $"\texplicit {Name}(";

				for (int y = 0, i = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++, i++)
					{
						str += $"{type.type.Name} _{Util.Components[y]}{Util.Components[x]}";
						str += (i != (width * height) - 1) ? ", " : "";
					}
				}
				str += ") : ";
				for (int y = 0; y < height; y++)
				{
					str += $"{Util.Components[y]}(";
					for (int x = 0; x < width; x++)
					{
						str += $"_{Util.Components[y]}{Util.Components[x]}";
						str += (x != width - 1) ? ", " : "";
					}
					str += ")";
					str += (y != height - 1) ? ", " : "";

				}
				str += " {}\n";

				return str;
			}
		}

		public string OperatorMul
		{
			get
			{
				string str = "";

				int size = Math.Max(width, height);

				str += $"\tfriend {Name} operator * (const {Name}& a, const {Name}& b)";
				str += $" {{ return {Name}(";
				for (int y = 0, i = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++, i++)
					{
						string substitute = x == y ? "1" : "0";

						string s = "";
						for (int c = 0; c < size; c++)
						{
							s += (c < height) ? $"a.row({y})[{c}]" : substitute;
							s += " * ";
							s += (c < width) ? $"b.col({x})[{c}]" : substitute;

							if (c != size - 1)
								s += " + ";
						}
						// str += "\n";
						str += s;
						str += (i != (width * height) - 1) ? ", " : "";
					}
				}
				str += "); }";
				str += "\n";
				return str;
			}
		}
		public string IndexOperator
		{
			get
			{
				string str = "";
				str += $"\t{type.Name} operator [] (i32 i) const {{ return c[i]; }}\n";
				str += $"\t{type.Name}& operator [] (i32 i) {{ return c[i]; }}\n";
				return str;
			}
		}
		public string Operators
		{
			get
			{
				string str = "";

				str += IndexOperator;
				str += OperatorMul;

				return str;
			}
		}

		public string RowColFunctions
		{
			get
			{
				string str = "";

				str += $"\t{type.Name} row(int i) const {{ return {type.Name}(";
				for (int x = 0; x < width; x++)
				{
					str += $"c[i].{Util.Components[x]}";
					str += (x != width - 1) ? ", " : "";

				}
				str += $"); }}\n";

				str += $"\t{ColName} col(int i) const {{ return {ColName}(";
				for (int x = 0; x < height; x++)
				{
					str += $"{Util.Components[x]}.c[i]";
					str += (x != height - 1) ? ", " : "";

				}
				str += $"); }}\n";

				return str;
			}
		}

		public string IdentityFunction
		{
			get
			{
				string str = "";
				str += $"\tstatic {Name} identity() {{ return {Name}(";
				for (int y = 0, i = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++, i++)
					{
						str += x == y ? "1" : "0";
						str += (i != (width * height) - 1) ? ", " : "";
					}
				}
				str += $"); }}\n";
				return str;
			}
		}
		public string TranslateFunction
		{
			get
			{
				string str = "";
				str += $"\tstatic {Name} translate(const f32_3& t) {{ return {Name}(";
				for (int y = 0, i = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++, i++)
					{
						if (y == height - 1 && x < 3)
						{
							str += $"t.{Util.Components[x]}";
						}
						else
							str += x == y ? "1" : "0";

						str += (i != (width * height) - 1) ? ", " : "";
					}
				}
				str += $"); }}\n";
				return str;
			}
		}

		public string Functions
		{
			get
			{
				string str = "";


				str += RowColFunctions;
				str += IdentityFunction;
				str += TranslateFunction;
				return str;
			}
		}

		public string Declaration
		{
			get
			{
				string str = $"export struct {Name}\n{{\n";
				str += "\tunion\n\t{\n";
				str += $"\t\t{type.Name} c[{height}];\n";
				str += $"\t\tstruct {{ {type.Name} ";
				for (int i = 0; i < height; i++)
				{
					str += $"{Util.Components[i]}";
					str += (i != height - 1) ? ", " : "";
				}
				str += "; };\n";
				str += "\t};\n";

				str += Ctors;
				str += Operators;
				str += Functions;

				str += "};\n";


				return str;
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

			List<Matrix> matrices = new List<Matrix>();
			foreach (var v in vectors)
			{
				// make matrices for only a few of the types
				if (v.type.type == Scalar.Type.Float && v.type.bitness == 32)
				{
					if (v.width == 3)
					{
						matrices.Add(new Matrix { type = v, height = 3 });
						matrices.Add(new Matrix { type = v, height = 4 });
					}
					else if (v.width == 4)
					{
						matrices.Add(new Matrix { type = v, height = 4 });
					}
				}
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

			foreach (var v in matrices)
				code += v.Declaration;


			string path = "types.ixx";
			if (args.Length > 1)
			{
				path = args[1];
			}

			path = "D:\\GIT\\uniwampus\\RHI\\types.ixx";

			System.IO.File.WriteAllText(path, code);
		}
	};
}