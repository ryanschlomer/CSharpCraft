

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




window.GetCameraComponent = {
    registerComponent: function (component) {
        window.cameraComponent = component;
        // Once the component is registered, call the updateCameraPosition function
    }
};

//async function sendCameraMovementToServer(xDelta, yDelta, zDelta) {
    
//    const direction = new THREE.Vector3();
//    camera.getWorldDirection(direction);
//    direction.y = 0; // Ignore the vertical component for horizontal movement
//    direction.normalize();
//    console.log("Before MoveCamera Camera.position: ", camera.position)
//    console.log("Before MoveCamera Camera.direction: ", direction)
//    return await window.cameraComponent.invokeMethodAsync('MoveCamera', xDelta, yDelta, zDelta, direction.x, direction.y, direction.z);
    
//}



//async function updateCameraPosition() {
//    const moveSpeed = 0.1;
//    const direction = new THREE.Vector3();
//    const right = new THREE.Vector3();
//    camera.getWorldDirection(direction);
//    direction.y = 0; // Ignore the vertical component for horizontal movement
//    direction.normalize();
//    right.crossVectors(direction, camera.up).normalize();

//    let xDelta = 0, yDelta = 0, zDelta = 0;

//    // Adjust vertical position independently of the camera's direction
//    if (keyStates['ShiftLeft'] || keyStates['ShiftRight']) {
//        if (keyStates['ArrowUp']) yDelta += moveSpeed;  // Move down
//        if (keyStates['ArrowDown']) yDelta -= moveSpeed;  // Move up
//    } else {
//        if (keyStates['ArrowUp']) zDelta += moveSpeed;  // Move backward
//        if (keyStates['ArrowDown']) zDelta -= moveSpeed;  // Move forward
//    }

//    // Determine the desired movement based on keyboard input
   
  
//    if (keyStates['ArrowLeft']) xDelta -= moveSpeed;
//    if (keyStates['ArrowRight']) xDelta += moveSpeed;
 
   

//    if (xDelta === 0 && yDelta === 0 && zDelta === 0)
//        return;

    
//    // Send the desired movement to the server and handle the return value
//    var newPosition = await sendCameraMovementToServer(xDelta, yDelta, zDelta);

//    if (newPosition && typeof newPosition.x === "number" && typeof newPosition.y === "number" && typeof newPosition.z === "number") {
//        camera.position.set(newPosition.x, newPosition.y, newPosition.z);
//    } else {
//        console.error("Invalid position data received", newPosition, typeof newPosition.X);
//    }


//    console.log("After MoveCamera C# position: ", newPosition)
//    console.log("After MoveCamera Camera.position: ", camera.position)
//    console.log("After MoveCamera Camera.direction: ", camera.direction)
 
//}







function addEventListeners() {
    let rotationSpeed = 0.005;

    controls.addEventListener('change', function () {
        // This event is fired when camera rotation changes
    });

    window.addEventListener('keydown', function (e) {
        keyStates[e.code] = true;
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
