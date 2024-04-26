
var scene, camera, renderer;
var controls; // Controls initialized later
var keyStates = {};
var textureLoader = new THREE.TextureLoader();
var defaultMaterial = new THREE.MeshBasicMaterial({ color: 0xFFFFFF }); // White material

var blockTextures = {}; // Stores materials for each block type

const blockGeometry = new THREE.BoxGeometry(1, 1, 1);

function initialize3DScene(canvasId) {
    const canvas = document.getElementById(canvasId);
    scene = new THREE.Scene();
    camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
    renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);

    controls = new THREE.PointerLockControls(camera, canvas);
    document.body.addEventListener('click', () => controls.lock());

    addEventListeners();


    animate();
}

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
        cameraPositionElement.innerHTML = `(X: ${camera.position.x.toFixed(0)}, Y: ${camera.position.y.toFixed(0)}, Z: ${camera.position.z.toFixed(0) })`;

    } catch (error) { }
}

async function getOrCreateMaterial(block) {
    if (!blockTextures[block.Type]) {
        // Use default material temporarily
        blockTextures[block.Type] = defaultMaterial;

        try {
            // Fetch texture data
            const textures = await fetchTextureData(block.Type);
            // Create materials from textures
            const materials = await createMaterialFromTextures(textures);
            // Update blockTextures with the newly created materials
            blockTextures[block.Type] = materials;
            // Update scene materials to apply the new materials to existing blocks
            updateSceneMaterials(block.Type);
        } catch (error) {
            //console.error("Error loading textures for block type:", block.Type, error);
            // Return default material in case of error
            return defaultMaterial;
        }
    }
    // Return the material for the block (default or loaded)
    return blockTextures[block.Type];
}






function fetchTextureData(type) {
    const url = `/api/textures/${type}`;
    //console.log(`Fetching texture data from: ${url}`);
    return fetch(url)
        .then(response => {
            if (!response.ok) {
                //throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            //console.log('Received texture data:', data);
            return data;
        })
        .catch(error => {
            //console.error('Error fetching texture data:', error);
        });
}


function loadTextureNearestFilter(imagePath) {
    return new Promise((resolve, reject) => {
        new THREE.TextureLoader().load(imagePath,
            texture => {
                texture.minFilter = THREE.NearestFilter;
                texture.magFilter = THREE.NearestFilter;
                resolve(texture);
            },
            undefined,
            error => reject(error)
        );
    });
}

async function createMaterialFromTextures(textures) {

    try {
        const sideTexture = await loadTextureNearestFilter(textures.side);
        const topTexture = await loadTextureNearestFilter(textures.top);
        const bottomTexture = await loadTextureNearestFilter(textures.bottom);

        return [
            new THREE.MeshBasicMaterial({ map: sideTexture }),
            new THREE.MeshBasicMaterial({ map: sideTexture }),
            new THREE.MeshBasicMaterial({ map: topTexture }),
            new THREE.MeshBasicMaterial({ map: bottomTexture }),
            new THREE.MeshBasicMaterial({ map: sideTexture }),
            new THREE.MeshBasicMaterial({ map: sideTexture })
        ];
    } catch (error) {
        //console.error("Failed to create materials:", error);
        return [defaultMaterial, defaultMaterial, defaultMaterial, defaultMaterial, defaultMaterial, defaultMaterial];
    }
}



function updateSceneMaterials(blockType) {
    scene.traverse(object => {
        if (object.isMesh && object.blockType === blockType) {
            object.material = blockTextures[blockType] || defaultMaterial;
            object.material.needsUpdate = true; // This is critical
        }
    });
}

function clearChunks(chunksToRemove) {
    //console.log("Chunks to remove:", chunksToRemove);

    if (!scene) {
        //console.error("Scene is undefined. Cannot traverse.");
        return;
    }

    const objectsToRemove = [];

    scene.traverse((object) => {
        if (object.userData.isChunk && chunksToRemove.includes(object.name)) {
            objectsToRemove.push(object);
        }
    });

    objectsToRemove.forEach(object => {
        // Dispose of the material and textures if they exist
        if (object.material) {
            if (Array.isArray(object.material)) {
                // For objects with multiple materials
                object.material.forEach(mat => {
                    if (mat.map) mat.map.dispose();
                    mat.dispose();
                });
            } else {
                // For objects with a single material
                if (object.material.map) object.material.map.dispose();
                object.material.dispose();
            }
        }

        // Dispose of the geometry
        if (object.geometry) {
            object.geometry.dispose();
        }

        // Finally, remove the object from the scene
        scene.remove(object);
    });

    //console.log("Removed objects:", objectsToRemove.length);
}


const chunkQueue = []; // Queue to store chunks for sequential rendering

function renderChunks(canvasId, jsonUpdatePayload) {
    const updatePayload = JSON.parse(jsonUpdatePayload);
    clearChunks(updatePayload.ChunksToRemove); // Remove old chunks based on IDs

    // Enqueue new chunks for rendering
    updatePayload.NewChunks.forEach(chunk => {
        chunkQueue.push(chunk);
    });

    // If no rendering is ongoing, start rendering the next chunk
    if (chunkQueue.length === updatePayload.NewChunks.length) {
        renderNextChunk();
    }
}

async function renderNextChunk() {
    if (chunkQueue.length === 0) return; // No more chunks to render

    const chunk = chunkQueue.shift(); // Dequeue the next chunk
    const chunkSize = chunk.Size;

    // Loop through all blocks within the chunk
    for (let index = 0; index < chunk.Blocks.length; index++) {
        const block = chunk.Blocks[index];

        if (block) {
            const x = index % chunkSize;
            const y = Math.floor(index / (chunkSize * chunkSize));
            const z = Math.floor((index % (chunkSize * chunkSize)) / chunkSize);

            // Create or get material for the block
            const material = await getOrCreateMaterial(block);

           
            // Create mesh for the block
            const mesh = new THREE.Mesh(blockGeometry, material);
            mesh.position.set(x + chunk.ChunkX * chunkSize, y, z + chunk.ChunkZ * chunkSize);
            mesh.blockType = block.Type;
            mesh.name = chunk.ChunkId; // Use Chunk ID as the name for easier removal

            mesh.userData.isChunk = true;

            mesh.geometry.computeBoundingSphere();


            scene.add(mesh);

            if (blockTextures[block.Type] !== defaultMaterial) {
                mesh.material = blockTextures[block.Type];
                mesh.material.needsUpdate = true;
            }
        }
    }

    // Render the next chunk after a short delay to prevent blocking the main thread
    setTimeout(renderNextChunk, 10);
}



function animate() {
    requestAnimationFrame(animate);
    updateCameraPosition();

    scene.traverse(object => {
        if (object.isMesh) {
            if (!object.material || object.material.map === undefined) {
                object.material = defaultMaterial; // Fallback to default material
            }
        }
    });

    renderer.render(scene, camera);

    sendCameraPositionToBlazor();
}





function adjustCanvasSize(canvasId) {
    const canvas = document.getElementById(canvasId);
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}

function requestFullScreen(elementId) {
    var element = document.getElementById(elementId);
    if (element.requestFullscreen) {
        element.requestFullscreen();  // Standard method
    } else if (element.webkitRequestFullscreen) {
        element.webkitRequestFullscreen(); // Chrome, Safari and Opera
    } else if (element.msRequestFullscreen) {
        element.msRequestFullscreen(); // IE/Edge
    }
}

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

