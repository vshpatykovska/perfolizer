﻿using System;
using CommandLine;
using CommandLine.Text;

namespace Perfolizer.Tool
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var parser = new Parser(settings => { settings.CaseInsensitiveEnumValues = true; });
                var parserResult = parser.ParseArguments<
                    ChangePointDetectorOptions,
                    HistogramBuilderOptions,
                    RqqOptions,
                    SelectorOptions,
                    MValueCalculatorOptions,
                    QuantileEstimatorOptions,
                    ShiftFunctionOptions,
                    RatioFunctionOptions
                >(args);

                int exitCode = 0;
                parserResult
                    .WithParsed<ChangePointDetectorOptions>(ChangePointDetectorOptions.Run)
                    .WithParsed<HistogramBuilderOptions>(HistogramBuilderOptions.Run)
                    .WithParsed<RqqOptions>(RqqOptions.Run)
                    .WithParsed<SelectorOptions>(SelectorOptions.Run)
                    .WithParsed<MValueCalculatorOptions>(MValueCalculatorOptions.Run)
                    .WithParsed<QuantileEstimatorOptions>(QuantileEstimatorOptions.Run)
                    .WithParsed<ShiftFunctionOptions>(ShiftFunctionOptions.Run)
                    .WithParsed<RatioFunctionOptions>(RatioFunctionOptions.Run)
                    .WithNotParsed(errs =>
                    {
                        PrintHelp(parserResult);
                        exitCode = 1;
                    });

                return exitCode;
            }
            catch (InvalidOperationException e)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static void PrintHelp(ParserResult<object> parserResult)
        {
            var helpText = HelpText.AutoBuild(parserResult, h =>
            {
                h.Heading = string.Empty;
                h.Copyright = string.Empty;
                h.AutoVersion = false;
                h.AddEnumValuesToHelpText = true;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e => e);
            Console.Error.WriteLine(helpText);
        }
    }
}