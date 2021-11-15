function stack() {

    var newFile = jsArguments[0];
    var stackFile = jsArguments[1];
    var outputDir = jsArguments[2];

    console.writeln("newFile = " + newFile);
    console.writeln("stackFile = " + stackFile);
    console.writeln("outputDir = " + outputDir);

    var newInfo = new FileInfo(newFile);
    var stackInfo = new FileInfo(stackFile);


    if(newInfo.isFile && stackInfo.isFile) {
        var newWindow = ImageWindow.open(newFile);
        var stackWindow = ImageWindow.open(stackFile);

        /* Retrieve header info about number of images */
        var totalFiles = 2;
        var stackNumberHeader;

        var header = stackWindow[0].keywords;

        for(let i = 0; i < header.length; i++) {
            let entry = header[i];
            if(entry.name.trim() == "STACKNO") {     
                stackNumberHeader = entry;
            }
         }

        if (stackNumberHeader) {
            totalFiles = parseInt(stackNumberHeader.value) + 1;
            console.writeln("Found keyword STACKNO. Total number of frames: " + totalFiles);
        
         } else {
            console.writeln("STACKNO keyword not found. Creating keyword");
            stackNumberHeader = new FITSKeyword();
            stackNumberHeader.name = "STACKNO";
            header.push(stackNumberHeader);
         }

         stackNumberHeader.value = totalFiles;

         // Store updpated header in stack
         stackWindow[0].keywords = header;


        var newId = newWindow[0].mainView.id;
        var stackId = stackWindow[0].mainView.id;

        var P = new PixelMath;
        P.expression = "iif(" + newId + ">(" + stackId + "*2), " + stackId + ", iif(" + newId + "==0, " + stackId + ", ((" + stackId + "*" + (totalFiles - 1) + ")+" + newId + ")/" + totalFiles + "))";
        P.expression1 = "";
        P.expression2 = "";
        P.expression3 = "";
        P.useSingleExpression = false;
        P.symbols = "";
        P.clearImageCacheAndExit = false;
        P.cacheGeneratedImages = false;
        P.generateOutput = true;
        P.singleThreaded = false;
        P.optimization = true;
        P.use64BitWorkingImage = false;
        P.rescale = false;
        P.rescaleLower = 0;
        P.rescaleUpper = 1;
        P.truncate = true;
        P.truncateLower = 0;
        P.truncateUpper = 1;
        P.createNewImage = false;
        P.showNewImage = false;
        P.newImageId = "";
        P.newImageWidth = 0;
        P.newImageHeight = 0;
        P.newImageAlpha = false;
        P.newImageColorSpace = PixelMath.prototype.SameAsTarget;
        P.newImageSampleFormat = PixelMath.prototype.SameAsTarget;
        /*
        * Read-only properties
        *
        P.outputData = [ // globalVariableId, globalVariableRK, globalVariableG, globalVariableB
        ];
        */

        P.executeOn(stackWindow[0].mainView);

        stackWindow[0].save();

        newWindow[0].forceClose();
        stackWindow[0].forceClose();

        File.writeTextFile(outputDir + "/done.txt", totalFiles.toString());
    } else {
        console.writeln("Input files do not seem to exist");

        File.writeTextFile(outputDir + "/error.txt", "error");
    }

}

stack();
