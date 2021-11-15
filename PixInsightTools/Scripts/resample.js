function resample() {

    var resampleFile = jsArguments[0];
    var amount = jsArguments[1];
    var outputFile = jsArguments[2];
    var outputDir = jsArguments[3];


    console.writeln("resampleFile = " + resampleFile);
    console.writeln("amount = " + amount);
    console.writeln("outputFile = " + outputFile);
    console.writeln("outputDir = " + outputDir);

    var resampleFileInfo = new FileInfo(resampleFile);

    if (resampleFileInfo.isFile) {

        var resampleWindow = ImageWindow.open(resampleFile);

        var P = new IntegerResample;
        P.zoomFactor = amount * -1;
        P.downsamplingMode = IntegerResample.prototype.Average;
        P.xResolution = 72.000;
        P.yResolution = 72.000;
        P.metric = false;
        P.forceResolution = false;
        P.noGUIMessages = true;

        P.executeOn(resampleWindow[0].mainView);
        resampleWindow[0].saveAs(outputFile, false, false, false, false);
        resampleWindow[0].forceClose();

        console.writeln("Writing textfile: " + outputDir + "/done_resample.txt");
        File.writeTextFile(outputDir + "/done_resample.txt", "done");

    } else {
        console.writeln("Input file does not seem to exist");

        console.writeln("Writing textfile: " + outputDir + "/error_resample.txt");
        File.writeTextFile(outputDir + "/error_resample.txt", "error");
    }
}

resample();


