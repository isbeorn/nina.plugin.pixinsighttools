function resample() {

    let guid = jsArguments[0].substring(1, jsArguments[0].length - 1);
    let resampleFile = jsArguments[1];
    let amount = jsArguments[2];
    let outputFile = jsArguments[3];
    let outputDir = jsArguments[4];


    console.writeln("guid = " + guid);
    console.writeln("resampleFile = " + resampleFile);
    console.writeln("amount = " + amount);
    console.writeln("outputFile = " + outputFile);
    console.writeln("outputDir = " + outputDir);

    let resampleFileInfo = new FileInfo(resampleFile);

    if (resampleFileInfo.isFile) {

        let resampleWindow = ImageWindow.open(resampleFile);

        let P = new IntegerResample;
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

        console.writeln("Writing textfile: " + outputDir + "/" + guid + "_" + "done_resample.txt");
        File.writeTextFile(outputDir + "/" + guid + "_" + "done_resample.txt", "done");

    } else {
        console.writeln("Input file does not seem to exist");

        console.writeln("Writing textfile: " + outputDir + "/" + guid + "_" + "error_resample.txt");
        File.writeTextFile(outputDir + "/" + guid + "_" + "error_resample.txt", "error");
    }
}

resample();


