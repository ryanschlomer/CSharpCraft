
document.addEventListener('DOMContentLoaded', function () {
    addTouchListeners();
});

function addTouchListeners() {
    // Ensure listeners are only added once
    if (!addTouchListeners.added) {
        document.addEventListener('touchstart', handleTouchStart, { passive: false });
        document.addEventListener('touchmove', handleTouchMove, { passive: false });
        document.addEventListener('touchend', handleTouchEnd, { passive: false });
        addTouchListeners.added = true;
    }
}

//camera and mining flags

var cameraStartTouch = null; //stores initial camera touch.
var isCameraMoving = false; // flag set when camera is moving. Cannot mine when moving
var isMining = false; //flag set when camera touch is held down. This starts mining and not camera moving
var miningTimer = null; //timer that calls handleMining function. Is set when camera touch point starts.
var updateMiningTimer = null;

//UI action flags
var controlsAreaHold = false;
var sneakDoubleClick = false
var sneakDoubleClickTimerStart = Date.now();
var moveDoubleClick = false;
var moveDoubleClickTimerStart = Date.now();
var jumpDoubleClick = false;
var jumpDoubleClickTimerStart = Date.now();
var isFlying = false;
var isJumping = false;


let activeTouches = {}; //might not need this
const touchThreshold = 10; // Threshold for detecting movement
const touchDurationThreshold = 300; // Threshold for detecting a hold (in milliseconds)
const doubleClickThreshold = 300; // Threshold for detecting a double click (in milliseconds)

//Notes:
//TODO: Flying messes up jump

//TODO: Click and hold for camera

function handleTouchStart(event) {
    event.preventDefault();

    for (let i = 0; i < event.touches.length; i++) {


        const touch = event.touches[i];
        console.log(touch);
        const touchData = {
            touch: touch,
            startX: touch.clientX,
            startY: touch.clientY,
            startTime: new Date().getTime(),
            lastTapTime: 0,
            isDoubleClick: false,
            isHold: false,
            isMove: false,
            clickedElement: null,
            initialAction: "Other", // default, could be an enum
        };

        activeTouches[touch.identifier] = touchData;



        if (isWithinElementArea(touch.clientX, touch.clientY, 'controls')) {
            var clickedElement = document.elementFromPoint(touch.clientX, touch.clientY);

            if (clickedElement.id === "sneakButton") {
                activeTouches[touch.identifier].initialAction = "Sneak";
                let elapsedTime = Date.now() - sneakDoubleClickTimerStart;
                if (elapsedTime < doubleClickThreshold) {
                    console.log("Sneak");
                    
                    handleSneak();
                }
                else {
                    //Future use
                }//reset Timer
                sneakDoubleClickTimerStart = Date.now();
            }
            else if (!controlsAreaHold) {
                controlsAreaHold = true;

                activeTouches[touch.identifier].initialAction = "Controls";
                let elapsedTime = Date.now() - moveDoubleClickTimerStart;
                if (elapsedTime < doubleClickThreshold) {
                    console.log("Run");
                    moveDoubleClick = true;
                    handleWalk(clickedElement);
                }
                else {
                    console.log("Walk");
                    moveDoubleClick = false;
                    handleWalk(clickedElement);
                }
                //reset Timer
                moveDoubleClickTimerStart = Date.now();
                controlsAreaHold = true;
            }


            activeTouches[touch.identifier].clickedElement = clickedElement;

        } else if (isWithinElementArea(touch.clientX, touch.clientY, 'jumpButtonContainer')) {
            activeTouches[touch.identifier].clickedElement = document.elementFromPoint(touch.clientX, touch.clientY);
            activeTouches[touch.identifier].initialAction = "Jump";
            //Every time you click Jump, check if it's been clicked within the threashold.
            //If so, it's a double jump click
            let elapsedTime = Date.now() - jumpDoubleClickTimerStart;
            if (elapsedTime < doubleClickThreshold) {
                isFlying = !isFlying;
                if (isFlying) {
                    console.log("Fly");
                    activeTouches[touch.identifier].initialAction = "Fly";
                    handleFly();
                }
                else {
                    console.log("Stopped Fly");
                    activeTouches[touch.identifier].initialAction = "";  //not sure if we need anything here
                    handleFly();
                }
            }
            else if (!isFlying) {
                console.log("Jump");
                isJumping = true;
                handleJump();
            }
                //resetTimer
            jumpDoubleClickTimerStart = Date.now();

        } else if (isWithinElementArea(touch.clientX, touch.clientY, 'quickSelectBar')) {
            const images = document.querySelectorAll('img');
            images.forEach(img => img.classList.add('ignore-pointer-events'));
            var clickedElement = document.elementFromPoint(touch.clientX, touch.clientY);
            activeTouches[touch.identifier].clickedElement = clickedElement;
            activeTouches[touch.identifier].initialAction = "Hotbar";
            handleHotbarClick(clickedElement);
        }
        else {
            cameraStartTouch = activeTouches[touch.identifier]; //assume it's camera for now
            miningTimer = setInterval(() => {
                handleMining(touch.clientX, touch.clientY);
            }, touchDurationThreshold);
        }


        //console.log("activeTouches[touch.identifier]: ", activeTouches[touch.identifier]);

    }
}

function handleTouchMove(event) {
    event.preventDefault();

    for (let i = 0; i < event.touches.length; i++) {
        const touch = event.touches[i];
        const touchData = activeTouches[touch.identifier];

        var initialTouchElementId = touch.target.id;
       

        //console.log(touch);

        if (initialTouchElementId === "jumpButton") {

        }
        else if (initialTouchElementId === "upButton" ||
            initialTouchElementId === "downButton" ||
            initialTouchElementId === "leftButton" ||
            initialTouchElementId === "rightButton") {

            var element = document.elementFromPoint(touch.clientX, touch.clientY);
            handleWalk(element);
        }

        else if (initialTouchElementId === "canvas3D") {//camera or mining
            const moveX = touch.clientX;
            const moveY = touch.clientY;


            //See if touch has moved before miningTime function is called.
            if (Math.abs(moveX - touchData.startX) > touchThreshold || Math.abs(moveY - touchData.startY) > touchThreshold) {

                if (!isMining) { //miningTimer function sets isMining to true if called
                    clearInterval(miningTimer); //turn off mining timer
                    console.log("moving");
                    isCameraMoving = true;
                    handleMoveCamera(touch);
                }
                
            }
        }
       
    }
}

function handleTouchEnd(event) {
    event.preventDefault();

    for (let i = 0; i < event.changedTouches.length; i++) {
        const touch = event.changedTouches[i];
        //Note:  the event.changedTouches has the element that was initially clicked.
        //Use this info instead of the activeTouches list
        //I'm thinking we can get rid of that list now.'
        if (touch) {

            console.log("touch", touch);
            var initialTouchElementId = touch.target.id;

            if (initialTouchElementId === "jumpButton") {//jumping or flying button
                if (isFlying) {
                    //maybe don't need this isFlying part
                    console.log("Got to isFyling in Touch End. What to do here");
                    keyStates['Space'] = false;
                    space = false;
                }
                if (isJumping) {
                    keyStates['Space'] = false;
                    space = false;
                }
            }
            else if (initialTouchElementId === "upButton" ||
                initialTouchElementId === "downButton" ||
                initialTouchElementId === "leftButton" ||
                initialTouchElementId === "rightButton") {//moving ended

                walkingSound.stop();
                controlsAreaHold = false;
                keyStates['ArrowUp'] = false;
                keyStates['ArrowDown'] = false;
                keyStates['ArrowLeft'] = false;
                keyStates['ArrowRight'] = false;
                moveDoubleClick = false;
            }
            else if (initialTouchElementId === "canvas3D") {

                clearInterval(miningTimer); //turn off mining timer
                clearInterval(updateMiningTimer);


                if (!isCameraMoving && !isMining) { //place block
                    handlePlaceBlock(touch);
                }
                else if (isMining) {
                    //TODO: Need to maybe do some things
                    
                }

                isCameraMoving = false;
                isMining = false
            }
            else {
                console.log("initialTouchElementId Error: ", initialTouchElementId);
            }

            delete activeTouches[touch.identifier];
        }
    }
}

function handlePlaceBlock(touch) {

    console.log("PlaceBlock", touch.clientX, touch.clientY);
    if (PlayerSelectedItem.type === "Block") { //make sure they have a block selected
        placeBlock(touch.clientX, touch.clientY);
    }
}


let lastProgressSoundPlayed = 0;
function handleMining(clientX, clientY) {
    clearInterval(miningTimer);
    if (isMining)
        return;
    if (PlayerSelectedItem.type != "Tool")
        return;

    var miningStartTime = Date.now();
    var miningDuration = 2000;


    isMining = true;
    isCameraMoving = false; //shouldn't be moving anyway


    console.log("Mining");
    playMiningSound(); //initial sound

    updateMiningTimer = setInterval(() => {

        if (PlayerSelectedItem.type != "Tool") { //make sure they have a tool selected.
            clearInterval(updateMiningTimer);

            //TODO: Might need to reset block texture if we change it
            return;
        }
        lastProgressSoundPlayed = 0;
        updateRaycaster(clientX, clientY);
       
        const elapsedTime = Date.now() - miningStartTime;

        if (elapsedTime >= miningDuration) {
            console.log("Got here");
            playMiningSound();
            handleMiningComplete(clientX, clientY);
            selectedItemMesh.rotation.x = 0.2; //reset
            clearInterval(updateMiningTimer);
        } else {
            updateMiningProgress(elapsedTime / miningDuration, clientX, clientY);
        }
    }, 100); // Check progress every 100 ms

}

function handleMiningComplete(x, y) {
    isMining = false;
    console.log("Mining complete at coordinates:", x, y);
    // Your mining logic here, e.g., removing a block

    
    removeBlock();
}

const animationDuration = 1000; // Duration of one full swing cycle in milliseconds
const swingAmplitude = 0.5; // Amplitude of the swing in radians

function updateMiningProgress(progress, x, y) {

    const miningProgress = progress * 100;
    //add mining animation
    if (miningProgress > 45 && miningProgress < 50)
        playMiningSound();

    // Calculate elapsed time within the animation cycle
    const elapsedTime = Date.now() % animationDuration;

    // Use a sine wave to create a smooth swinging motion
    const swingRotation = Math.sin((elapsedTime / animationDuration) * Math.PI * 2) * swingAmplitude;

    // Apply the rotation to the pickaxe model
    selectedItemMesh.rotation.x = 0.2 + swingRotation; // Adjust the base rotation as needed


}
function playMiningSound() {
    if (miningSound.isPlaying) {
        miningSound.stop();
    }
    miningSound.play();
}

function handleHotbarClick(clickedSlot) {

    console.log("clickedSlot: ", clickedSlot);
    const digit = clickedSlot.id.replace('quick-select-slot', '');
    const slotIndex = parseInt(digit, 10) - 1; // Subtract 1 for zero-based index
    console.log(digit, slotIndex);
    // Adjust 'Digit0' to select the last slot (usually slot 10)
    if (digit === '0') {
        //Open Inventory eventually

    }

    selectSlot(slotIndex);
}

function handleJump() {

    keyStates['Space'] = true;
}
function handleFly() {
    keyStates['Fly'] = true;
    
}

function handleSneak() {
    sneakDoubleClick = !sneakDoubleClick;
    keyStates['Sneak'] = sneakDoubleClick;
}


function handleWalk(element) {

    keyStates['Run'] = moveDoubleClick;
    console.log("keyStates['Run']", keyStates['Run']);

    if (element.id === "upButton") {
        keyStates['ArrowUp'] = true;
        keyStates['ArrowDown'] = false;
        keyStates['ArrowLeft'] = false;
        keyStates['ArrowRight'] = false;
    }
    else if (element.id === "downButton") {
        keyStates['ArrowUp'] = false;
        keyStates['ArrowDown'] = true;
        keyStates['ArrowLeft'] = false;
        keyStates['ArrowRight'] = false;
    }

    else if (element.id === "leftButton") {
        keyStates['ArrowUp'] = false;
        keyStates['ArrowDown'] = false;
        keyStates['ArrowLeft'] = true;
        keyStates['ArrowRight'] = false;
    }
    else if (element.id === "rightButton") {
        keyStates['ArrowUp'] = false;
        keyStates['ArrowDown'] = false;
        keyStates['ArrowLeft'] = false;
        keyStates['ArrowRight'] = true;
    }

    playWalkingSound();
    
}

function playWalkingSound() {
    if (!walkingSound.isPlaying || walkingSoundEnded) {
        walkingSoundEnded = false;
        walkingSound.play();
        
    }
}




function handleMoveCamera(touch) {
    const initialTouch = activeTouches[touch.identifier];
    
   //there is no touch.initialAction on the touchMove touch
    console.log("touch.initialAction: ", initialTouch.initialAction);
    if (!touch)
        return; //touch ended already
    if (touch.initialAction === 'Controls') {

    }

    else { //camera move

        const deltaX = touch.clientX - cameraStartTouch.startX;
        const deltaY = touch.clientY - cameraStartTouch.startY;

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
        camera.updateProjectionMatrix();

        // Update cameraStartTouch for the next movement calculation
        cameraStartTouch.startX = touch.clientX;
        cameraStartTouch.startY = touch.clientY;
    }

    //console.log("Handling touch and move:", { x, y });
}








// Function to check if the touch coordinates are within the controls area
//This checks to see if the point is within the element
function isWithinElementArea(touchX, touchY, elementId) {
    // Get the bounding rectangle of the controls area
    const controlsArea = document.getElementById(elementId); // Assuming 'controls' is the ID of your controls area
    const controlsRect = controlsArea.getBoundingClientRect();

    //console.log("controlsArea: ", controlsArea);
    //console.log("controlsRect: ", controlsRect);
    // Check if the touch coordinates are within the controls area rectangle
    const isWithin =
        touchX >= controlsRect.left &&
        touchX <= controlsRect.right &&
        touchY >= controlsRect.top &&
        touchY <= controlsRect.bottom;
    //console.log("isWithin: " + isWithin);
    return isWithin;
}


//var controlsTouchTarget; // Variable to track the touch target for controls
//var cameraStartTouch; // Variable to track the starting touch point for camera rotation
//var cameraMoveTouch; // Variable to track the current touch point for camera rotation; not sure we need this
//var touchStartedInMovingControls = false; // Flag to indicate if touch started in controls area

//function addTouchListeners() {

//    document.addEventListener('touchstart', handleTouchStart, { passive: false });
//    document.addEventListener('touchmove', handleTouchMove, { passive: false });
//    document.addEventListener('touchend', handleTouchEnd, { passive: false });

//    function handleTouchStart(e) {
//        //document.addEventListener('touchstart', function (e) {
//        e.preventDefault();
//        // Loop through all touch points
//        for (let i = 0; i < e.touches.length; i++) {
//            const touch = e.touches[i];
//            // Determine the initial touch target

//            // Check if the initial touch target is within the controls area
//            if (isWithinControlsArea(touch.clientX, touch.clientY, 'controls')) {

//                touchStartedInMovingControls = true;
//                controlsTouchTarget = touch;
//                // Update key states based on the initial touch target
//                updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
//                // Break the loop since we found the touch within the controls area
//                continue;
//            }
//            else if (isWithinControlsArea(touch.clientX, touch.clientY, 'jumpButtonContainer')) {

//                //touchStartedInMovingControls = true;
//                //controlsTouchTarget = touch;
//                // Update key states based on the initial touch target
//                updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
//                // Break the loop since we found the touch within the controls area
//                continue;
//            }
//            else if (isWithinControlsArea(touch.clientX, touch.clientY, 'quickSelectBar')) {

//                //ignore images to get the elementFromPoint
//                const images = document.querySelectorAll('img');
//                images.forEach(img => img.classList.add('ignore-pointer-events'));

//                // Update key states based on the initial touch target
//                updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
//                continue;
//            }
//            else {
//                // Store touch point for camera rotation
//                cameraStartTouch = touch;
//                updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));

//                //in here, how can we tell if it's a "touch" or a "touch and hold for camera rotation"?
//                updateRaycaster(touch.clientX, touch.clientY);
//                updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
//            }
//        }

//        //  });
//    }



//    function handleTouchMove(e) {
//        //document.addEventListener('touchmove', function (e) {
//        e.preventDefault();
//        // Loop through all touch points
//        for (let i = 0; i < e.touches.length; i++) {
//            const touch = e.touches[i];

//            //TODO: For Some reason the camera touch can interfere with the arrow controls touch

//            // Check if the touch is within the controls area
//            if (controlsTouchTarget) { //Do we have an arrow controlsTouchTarget?
//                if (touch.identifier === controlsTouchTarget.identifier) { //Is this touch point the arrow controls touch target?
//                    if (isWithinControlsArea(touch.clientX, touch.clientY, 'controls')) { //Are we still inside the arrow controls?
//                        // Update key states for moving controls
//                        updateKeyStates(document.elementFromPoint(touch.clientX, touch.clientY));
//                    }
//                    else { //no longer in the arrow controls
//                        keyStates['ArrowUp'] = false;
//                        keyStates['ArrowDown'] = false;
//                        keyStates['ArrowLeft'] = false;
//                        keyStates['ArrowRight'] = false;
//                    }
//                    continue; //continue checking for other touch targets
//                }
//            }
//            if (cameraStartTouch) { //If there is a start touch, then camera is rotating
//                if (touch.identifier === cameraStartTouch.identifier) { //Is this touch the camera rotation

//                    console.log("Touch count" + e.touches.length);
//                    console.log(cameraStartTouch.clientX, cameraStartTouch.clientY);

//                    const deltaX = touch.clientX - cameraStartTouch.clientX;
//                    const deltaY = touch.clientY - cameraStartTouch.clientY;

//                    // Adjust the rotation sensitivity based on your preference
//                    const rotationSensitivity = 0.002;

//                    // Calculate rotation angles based on touch movement
//                    const rotationY = deltaX * rotationSensitivity; // Rotate around world Y coordinate
//                    const rotationX = deltaY * rotationSensitivity; // Rotate camera up and down (pitch)


//                    //I am not sure if this is how you correctly set the rotation, but it seems to work. :)
//                    camera.rotation.order = "YXZ"; //Don't know if I need this or not

//                    camera.rotation.x -= rotationX;
//                    camera.rotation.y -= rotationY;

//                    const maxPitch = Math.PI / 2; // 90 degrees
//                    const minPitch = -Math.PI / 2; // -90 degrees
//                    // Clamp the pitch angle within the specified range
//                    camera.rotation.x = Math.max(Math.min(camera.rotation.x, maxPitch), minPitch);

//                    camera.rotation.z = 0; //Need to set this to 0 or there are issues.


//                    // Update cameraStartTouch for the next movement calculation
//                    cameraStartTouch = {
//                        identifier: touch.identifier,
//                        clientX: touch.clientX,
//                        clientY: touch.clientY
//                    };
//                }
//            }
//        }
//        //  });
//    }


//    function handleTouchEnd(e) {
//        //document.addEventListener('touchend', function (e) {
//        e.preventDefault();
//        for (let i = 0; i < e.changedTouches.length; i++) {
//            const touch = e.changedTouches[i];
//            if (controlsTouchTarget && touch.identifier === controlsTouchTarget.identifier) {
//                controlsTouchTarget = null;
//                touchStartedInMovingControls = false;

//                // Reset only if the controls touch has ended
//                keyStates['ArrowUp'] = false;
//                keyStates['ArrowDown'] = false;
//                keyStates['ArrowLeft'] = false;
//                keyStates['ArrowRight'] = false;
//            } else if (cameraStartTouch && touch.identifier === cameraStartTouch.identifier) {
//                cameraStartTouch = null;
//            }
//        }
//        //   });
//    }

//}

// Function to update key states based on the touch target
function updateKeyStates(targetElement) {
    keyStates['ArrowUp'] = targetElement.id === 'upButton';
    keyStates['ArrowDown'] = targetElement.id === 'downButton';
    keyStates['ArrowLeft'] = targetElement.id === 'leftButton';
    keyStates['ArrowRight'] = targetElement.id === 'rightButton';

    console.log(targetElement);
    if (targetElement.id === 'jumpButton') {
        keyStates['Space'] = true;
        //Need to set space to false 
        space = false;
    }
    if (targetElement.id.includes("quick-select-slot")) {

        if (targetElement.id.startsWith("quick-select-slot")) {
            const slotId = targetElement.id;
            const digit = slotId.replace('quick-select-slot', '');
            const slotIndex = parseInt(digit, 10) - 1; // Subtract 1 for zero-based index
            
            if (digit === '0') {
                //Open Inventory eventually

            } else {
                selectSlot(slotIndex);
            }
        }
    }
}





