function alignment() {
    var reference = jsArguments[0];
    var fileToCalibrate = jsArguments[1];
    var outputDir = jsArguments[2];

    console.writeln("reference = " + reference);
    console.writeln("fileToCalibrate = " + fileToCalibrate);
    console.writeln("outputDir = " + outputDir);

    var sa = new StarAlignment;
    sa.outputSampleFormat = StarAlignment.prototype.SameAsTarget;
    sa.writeKeywords = true;
    sa.outputPrefix = "";
    sa.outputPostfix = "_r";
    sa.outputExtension = ".xisf";
    sa.useFileThreads = true;
    sa.noGUIMessages = true;
    sa.generateMasks = false;
    sa.generateDrizzleData = false;
    sa.generateDistortionMaps = false;

    sa.outputDirectory = outputDir;

    sa.referenceImage = reference;
    sa.referenceIsFile = true;

    var isEnabled = true;
    var isFile = true;
    sa.targets = [[isEnabled, isFile, fileToCalibrate]];

    sa.executeGlobal();
    File.writeTextFile(outputDir + "/aligned.txt", "started");
}
alignment();

