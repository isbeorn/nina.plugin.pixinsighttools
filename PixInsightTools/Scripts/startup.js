var outputDir = jsArguments[0];

console.writeln("outputDir = " + outputDir);

File.writeTextFile(outputDir + "/started.txt", "started");