
function setCameraPosition(x, y, z) {
    camera.position.set(x, y, z);
}

function sendCameraPositionToBlazor() {
    if (!camera) return; // Make sure the camera is initialized
    DotNet.invokeMethodAsync('CSharpCraft', 'UpdateCameraInfo',
        camera.position.x, camera.position.y, camera.position.z)
        .catch(error => console.error('Failed to send camera position:', error));


}

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
    });


}

function updateCameraPosition() {
    const moveSpeed = 0.1;
    let direction = new THREE.Vector3();
    camera.getWorldDirection(direction);
    direction.y = 0; // Ignore the vertical component for horizontal movement
    direction.normalize();

    let right = new THREE.Vector3();
    right.crossVectors(direction, camera.up).normalize();

    // Adjust vertical position independently of the camera's direction
    if (keyStates['ShiftLeft'] || keyStates['ShiftRight']) {
        if (keyStates['ArrowUp']) camera.position.y += moveSpeed;
        if (keyStates['ArrowDown']) camera.position.y -= moveSpeed;
    } else {
        // Normal forward/backward and right/left movement
        if (keyStates['ArrowUp']) camera.position.addScaledVector(direction, moveSpeed);
        if (keyStates['ArrowDown']) camera.position.addScaledVector(direction, -moveSpeed);
    }

    // Left/right movement
    if (keyStates['ArrowLeft']) camera.position.addScaledVector(right, -moveSpeed);
    if (keyStates['ArrowRight']) camera.position.addScaledVector(right, moveSpeed);


    try {
        const cameraPositionElement = document.getElementById('cameraPosition');

        //display coordinates on screen
        cameraPositionElement.innerHTML = `(X: ${camera.position.x.toFixed(0)}, Y: ${camera.position.y.toFixed(0)}, Z: ${camera.position.z.toFixed(0)})`;

    } catch (error) { }
}