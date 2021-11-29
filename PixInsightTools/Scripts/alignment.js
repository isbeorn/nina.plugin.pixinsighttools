function alignment() {
    let reference = jsArguments[0];
    let fileToCalibrate = jsArguments[1];
    let outputDir = jsArguments[2];

    console.writeln("reference = " + reference);
    console.writeln("fileToCalibrate = " + fileToCalibrate);
    console.writeln("outputDir = " + outputDir);

    let sa = new StarAlignment;
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

    let isEnabled = true;
    let isFile = true;
    sa.targets = [[isEnabled, isFile, fileToCalibrate]];

    sa.executeGlobal();
    File.writeTextFile(outputDir + "/aligned.txt", "started");
}
alignment();

