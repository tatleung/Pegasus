﻿// -----------------------------------------------------------------------
// <copyright file="GenerateCodePass.cs" company="(none)">
//   Copyright © 2012 John Gietzen.  All Rights Reserved.
//   This source is subject to the MIT license.
//   Please see license.txt for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Pegasus.Compiler
{
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Reflection;
    using Pegasus.Expressions;

    internal class GenerateCodePass : CompilePass
    {
        public override void Run(Grammar grammar, CompileResult result)
        {
            using (var stringWriter = new StringWriter())
            using (var codeWriter = new IndentedTextWriter(stringWriter))
            {
                new GenerateCodeExpressionTreeWlaker(result, codeWriter).WalkGrammar(grammar);
                result.Code = stringWriter.ToString();
            }
        }

        private class GenerateCodeExpressionTreeWlaker : ExpressionTreeWalker
        {
            private readonly CompileResult result;
            private readonly IndentedTextWriter code;

            public GenerateCodeExpressionTreeWlaker(CompileResult result, IndentedTextWriter codeWriter)
            {
                this.result = result;
                this.code = codeWriter;
            }

            public override void WalkGrammar(Grammar grammar)
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                this.code.WriteLine("// -----------------------------------------------------------------------");
                this.code.WriteLine("// <auto-generated>");
                this.code.WriteLine("// This code was generated by " + assemblyName.Name + " " + assemblyName.Version);
                this.code.WriteLine("//");
                this.code.WriteLine("// Changes to this file may cause incorrect behavior and will be lost if");
                this.code.WriteLine("// the code is regenerated.");
                this.code.WriteLine("// </auto-generated>");
                this.code.WriteLine("// -----------------------------------------------------------------------");
                this.code.WriteLine();

                this.code.WriteLine("namespace Test");
                this.code.WriteLine("{");
                this.code.Indent++;

                this.code.WriteLine("[System.CodeDom.Compiler.GeneratedCode(Tool = \"" + assemblyName.Name + "\", Version = \"" + assemblyName.Version + "\")]");
                this.code.WriteLine("public partial class Parser");
                this.code.WriteLine("{");
                this.code.Indent++;

                this.code.WriteLine("public string Parse(string subject)");
                this.code.WriteLine("{");
                this.code.Indent++;
                this.code.WriteLine("return this." + grammar.Rules[0].Name + "(new Cursor(subject)).Value;");
                this.code.Indent--;
                this.code.WriteLine("}");

                base.WalkGrammar(grammar);

                this.code.Indent--;
                this.code.WriteLine("}");

                this.code.Indent--;
                this.code.WriteLine("}");
            }

            protected override void WalkRule(Rule rule)
            {
                this.code.WriteLine();
                this.code.WriteLine("private ParseResut<string> " + rule.Name + "(Cursor cursor)");
                this.code.WriteLine("{");
                this.code.Indent++;
                this.code.WriteLine("var startCursor = cursor;");

                base.WalkRule(rule);

                this.code.WriteLine();
                this.code.WriteLine("var len = cursor.Location - startCursor.Location;");
                this.code.WriteLine("return new ParseResult<string>(len, cursor.Subject.Substring(startCursor.Location, len));");
                this.code.Indent--;
                this.code.WriteLine("}");
            }

            protected override void WalkLiteralExpression(LiteralExpression literalExpression)
            {
                this.code.WriteLine("var l1 = this.ParseLiteral(" + ToLiteral(literalExpression.Value) + ", cursor);");
                this.code.WriteLine("if (l1 != null)");
                this.code.WriteLine("{");
                this.code.Indent++;
                this.code.WriteLine("cursor = cursor.Advance(l1);");
                this.code.Indent--;
                this.code.WriteLine("}");
            }

            protected override void WalkWildcardExpression(WildcardExpression wildcardExpression)
            {
                this.code.WriteLine("var c1 = this.ParseAny(cursor);");
                this.code.WriteLine("if (c1 != null)");
                this.code.WriteLine("{");
                this.code.Indent++;
                this.code.WriteLine("cursor = cursor.Advance(c1);");
                this.code.Indent--;
                this.code.WriteLine("}");
            }

            private static string ToLiteral(string input)
            {
                using (var writer = new StringWriter())
                {
                    using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                    {
                        provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                        return writer.ToString();
                    }
                }
            }
        }
    }
}
