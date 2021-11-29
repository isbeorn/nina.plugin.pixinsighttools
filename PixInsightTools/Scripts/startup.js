
let guid = jsArguments[0].substring(1, jsArguments[0].length - 1);
let outputDir = jsArguments[1];

console.writeln("guid = " + guid);
console.writeln("outputDir = " + outputDir);

File.writeTextFile(outputDir + "/" + guid + "_" + "started.txt", "started");