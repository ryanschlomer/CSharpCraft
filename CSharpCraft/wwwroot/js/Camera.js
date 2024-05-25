

var space = false;

async function sendInputToServer(deltaTime) {
    // Calculate movement deltas based on key states
    let xDelta = 0, yDelta = 0, zDelta = 0;
    if (keyStates['ArrowUp']) zDelta += 1;
    if (keyStates['ArrowDown']) zDelta -= 1;
    if (keyStates['ArrowLeft']) xDelta -= 1;
    if (keyStates['ArrowRight']) xDelta += 1;

    if (!space) {
        if (keyStates['Space']) {
            yDelta += 1; // Example for jump
            space = true;
        }
    }
    
    //might need to send in the keys in order to do other things like fly.


    // Assume camera.getWorldDirection provides the forward direction vector
    const direction = new THREE.Vector3();
    camera.getWorldDirection(direction);
    direction.y = 0; // Normalize Y to ignore vertical component for horizontal movement
    direction.normalize();

    //console.log("Got here: ",deltaTime, xDelta, yDelta, zDelta, direction);
    // Send the input to the server
    await connection.invoke("UpdatePlayer", deltaTime, xDelta, yDelta, zDelta, direction.x, direction.y, direction.z)
        .then(position => {
            if (position) {
                camera.position.set(position.x, position.y, position.z);
                //camera.lookAt(new THREE.Vector3(0, 0, 0)); // Or any other point you want the camera to focus on
            }
        })
        .catch(err => console.error("Error updating player: ", err.toString()));
}


connection.on("UpdatePosition", (newPosition) => {
    camera.position.set(newPosition.x, newPosition.y, newPosition.z);
});








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
    console.log("handleBlockInteraction");
    // Define block placement keys
    const blockPlacementKeys = [
        'Digit1', 'Digit2', 'Digit3', 'Digit4', 'Digit5',
        'Digit6', 'Digit7', 'Digit8', 'Digit9', 'Digit0'
    ];

    // Check if the key is for block placement
    if (blockPlacementKeys.includes(code)) {
        placeBlock(code); // Function to place a block
    }

    // Check if the key is the Delete key for block removal
    if (code === 'Delete') {
        removeBlock(); // Function to remove a block
    }
}

// Function to place a block
function placeBlock(code) {
    // if selectedCoords is not null and selectedNormal is not null
    if (selectedCoords && selectedNormal) { // verify block is selected
        // Calculate new block position
        var x = selectedCoords.x + selectedNormal.x - 0.5;
        var y = selectedCoords.y + selectedNormal.y - 0.5;
        var z = selectedCoords.z + selectedNormal.z - 0.5;

        console.log("Placing block at", x, y, z);

        // Invoke server method to place block
        connection.invoke("HandleBlockInteraction", x, y, z, code)
            .catch(err => console.error(err.toString()));
    }
}




