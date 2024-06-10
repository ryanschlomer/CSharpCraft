

var space = false;

async function sendInputToServer(deltaTime) {
   

    // Assume camera.getWorldDirection provides the forward direction vector
    const direction = new THREE.Vector3();
    camera.getWorldDirection(direction);
    direction.y = 0; // Normalize Y to ignore vertical component for horizontal movement
    direction.normalize();

    //console.log("Got here: ",deltaTime, xDelta, yDelta, zDelta, direction);
    // Send the input to the server
//    await connection.invoke("UpdatePlayer", deltaTime, xDelta, yDelta, zDelta, direction.x, direction.y, direction.z)

    const playerUpdateData = {
        DeltaTime: deltaTime,
        DirectionX: direction.x,
        DirectionY: direction.y,
        DirectionZ: direction.z,
        KeyStates: keyStates
    };


    await connection.invoke("UpdatePlayer", playerUpdateData)
        .then(position => {
            if (position) {
                camera.position.set(position.x, position.y, position.z);
                camera.updateProjectionMatrix();
            }
        })
        .catch(err => console.error("Error updating player: ", err.toString()));
}


//connection.on("UpdatePosition", (newPosition) => {
//    camera.position.set(newPosition.x, newPosition.y, newPosition.z);
//});








function addEventListeners() {
    let rotationSpeed = 0.005;

    controls.addEventListener('change', function () {
        // This event is fired when camera rotation changes
    });

    window.addEventListener('keydown', function (e) {

        keyStates[e.code] = true;

        // Handle block placement and removal
        handleBlockInteraction(e);
    });

    window.addEventListener('keyup', function (e) {
        keyStates[e.code] = false;
        if(e.code === 'Space')
            space = false;
    });

   

}


function setCameraPosition(x, y, z) {
    camera.position.set(x, y, z);
    console.log(camera.position);
}


function updateCameraPositionDisplay() {
    var cameraElement = document.getElementById('cameraPosition');
    if (cameraElement) {
        // Assuming 'camera' is your THREE.js camera object
        cameraElement.textContent = `Camera Position: (X: ${camera.position.x.toFixed(2)}, Y: ${camera.position.y.toFixed(2)}, Z: ${camera.position.z.toFixed(2)})`;
    }
}





// Function to handle block placement and removal
function handleBlockInteraction(event) {
    const { code } = event;
    console.log("code: ", code);

    // Define block placement keys
    const hotbarKeys = [
        'Digit1', 'Digit2', 'Digit3', 'Digit4', 'Digit5',
        'Digit6', 'Digit7', 'Digit8', 'Digit9', 'Digit0'
    ];

    // Check if the key is for block placement
    if (hotbarKeys.includes(code)) {
        // Remove the 'Digit' prefix and convert to integer
        const digit = code.replace('Digit', '');
        const slotIndex = parseInt(digit, 10) - 1; // Subtract 1 for zero-based index

        // Adjust 'Digit0' to select the last slot (usually slot 10)
        if (digit === '0') {
            //Open Inventory eventually

        }

        selectSlot(slotIndex);
    }
    //Check is key is for place block.
    else if (code === 'KeyP') { //not sure what the key should be
        if (PlayerSelectedItem.type === "Block") { //make sure they have a block selected
            //placeBlock(code);
            //TODO: maybe fix this in the future to allow placing blocks with keys
            //Or, just go all to touch screen.
        }
    }
    // Check if the key is the Delete key for block removal
    if (code === 'Delete') {
        if (PlayerSelectedItem.type === "Tool") { //make sure they have a tool selected.
            removeBlock(); // Function to remove a block
        }
    }
}

// Function to place a block
function placeBlock(clientX, clientY) {
    // if selectedCoords is not null and selectedNormal is not null
    updateRaycaster(clientX, clientY);

    console.log("clientX, clientY", clientX, clientY);

    console.log("selectedCoords && selectedNormal", selectedCoords, selectedNormal);


    if (selectedCoords && selectedNormal) { // verify block is selected
        // Calculate new block position
        var x = selectedCoords.x + selectedNormal.x - 0.5;
        var y = selectedCoords.y + selectedNormal.y - 0.5;
        var z = selectedCoords.z + selectedNormal.z - 0.5;

        console.log("Placing block at", x, y, z);


        // Invoke server method to place block
        connection.invoke("HandleBlockInteraction", x, y, z, PlayerSelectedItem.blockId.toString())
            .catch(err => console.error(err.toString()));
    }

}




