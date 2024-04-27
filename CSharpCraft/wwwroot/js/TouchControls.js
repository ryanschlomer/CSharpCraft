
// Function to check if the touch coordinates are within the controls area
function isWithinControlsArea(touchX, touchY) {
    // Get the bounding rectangle of the controls area
    const controlsArea = document.getElementById('controls'); // Assuming 'controls' is the ID of your controls area
    const controlsRect = controlsArea.getBoundingClientRect();

    // Check if the touch coordinates are within the controls area rectangle
    const isWithin =
        touchX >= controlsRect.left &&
        touchX <= controlsRect.right &&
        touchY >= controlsRect.top &&
        touchY <= controlsRect.bottom;
    //console.log("isWithin" + isWithin);
    return isWithin;
}


var controlsTouchTarget; // Variable to track the touch target for controls
var cameraStartTouch; // Variable to track the starting touch point for camera rotation
var cameraMoveTouch; // Variable to track the current touch point for camera rotation; not sure we need this
var touchStartedInMovingControls = false; // Flag to indicate if touch started in controls area

function addTouchListeners() {

    document.addEventListener('touchstart', handleTouchStart, { passive: false });
    document.addEventListener('touchmove', handleTouchMove, { passive: false });
    document.addEventListener('touchend', handleTouchEnd, { passive: false });

    function handleTouchStart(e) {
        //document.addEventListener('touchstart', function (e) {
        e.preventDefault();
        // Loop through all touch points
        for (let i = 0; i < e.touches.length; i++) {
            const touch = e.touches[i];
            // Determine the initial touch target

            // Check if the initial touch target is within the controls area
            if (isWithinControlsArea(touch.clientX, touch.clientY)) {

                touchStartedInMovingControls = true;
                controlsTouchTarget = touch;
                // Update key states based on the initial touch target
                updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
                // Break the loop since we found the touch within the controls area
                continue;
            }
            else {
                // Store touch point for camera rotation
                cameraStartTouch = touch;
            }
        }

        //  });
    }



    function handleTouchMove(e) {
        //document.addEventListener('touchmove', function (e) {
        e.preventDefault();
        // Loop through all touch points
        for (let i = 0; i < e.touches.length; i++) {
            const touch = e.touches[i];

            //TODO: For Some reason the camera touch can interfere with the arrow controls touch

            // Check if the touch is within the controls area
            if (controlsTouchTarget) { //Do we have an arrow controlsTouchTarget?
                if (touch.identifier === controlsTouchTarget.identifier) { //Is this touch point the arrow controls touch target?
                    if (isWithinControlsArea(touch.clientX, touch.clientY)) { //Are we still inside the arrow controls?
                        // Update key states for moving controls
                        updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
                    }
                    else { //no longer in the arrow controls
                        keyStates['ArrowUp'] = false;
                        keyStates['ArrowDown'] = false;
                        keyStates['ArrowLeft'] = false;
                        keyStates['ArrowRight'] = false;
                    }
                    continue; //continue checking for other touch targets
                }
            }
            if (cameraStartTouch) { //If there is a start touch, then camera is rotating
                if (touch.identifier === cameraStartTouch.identifier) { //Is this touch the camera rotation

                    console.log("Touch count" + e.touches.length);
                    console.log(cameraStartTouch.clientX, cameraStartTouch.clientY);

                    const deltaX = touch.clientX - cameraStartTouch.clientX;
                    const deltaY = touch.clientY - cameraStartTouch.clientY;

                    // Adjust the rotation sensitivity based on your preference
                    const rotationSensitivity = 0.002;

                    // Calculate rotation angles based on touch movement
                    const rotationY = deltaX * rotationSensitivity; // Rotate around world Y coordinate
                    const rotationX = deltaY * rotationSensitivity; // Rotate camera up and down (pitch)


                    //I am not sure if this is how you correctly set the rotation, but it seems to work. :)
                    camera.rotation.order = "YXZ"; //Don't know if I need this or not

                    camera.rotation.x -= rotationX;
                    camera.rotation.y -= rotationY;

                    const maxPitch = Math.PI / 2; // 90 degrees
                    const minPitch = -Math.PI / 2; // -90 degrees
                    // Clamp the pitch angle within the specified range
                    camera.rotation.x = Math.max(Math.min(camera.rotation.x, maxPitch), minPitch);

                    camera.rotation.z = 0; //Need to set this to 0 or there are issues.


                    // Update cameraStartTouch for the next movement calculation
                    cameraStartTouch = {
                        identifier: touch.identifier,
                        clientX: touch.clientX,
                        clientY: touch.clientY
                    };
                }
            }
        }
        //  });
    }


    function handleTouchEnd(e) {
        //document.addEventListener('touchend', function (e) {
        e.preventDefault();
        for (let i = 0; i < e.changedTouches.length; i++) {
            const touch = e.changedTouches[i];
            if (touch.identifier === controlsTouchTarget.identifier) {
                controlsTouchTarget = null;
                touchStartedInMovingControls = false;

                // Reset only if the controls touch has ended
                keyStates['ArrowUp'] = false;
                keyStates['ArrowDown'] = false;
                keyStates['ArrowLeft'] = false;
                keyStates['ArrowRight'] = false;
            } else if (touch.identifier === cameraStartTouch.identifier) {
                cameraStartTouch = null;
            }
        }
        //   });
    }

}

// Function to update key states based on the touch target
function updateKeyStates(targetElement) {
    keyStates['ArrowUp'] = targetElement.id === 'upButton';
    keyStates['ArrowDown'] = targetElement.id === 'downButton';
    keyStates['ArrowLeft'] = targetElement.id === 'leftButton';
    keyStates['ArrowRight'] = targetElement.id === 'rightButton';
}



document.addEventListener('DOMContentLoaded', function () {
    addTouchListeners();
});

