// Three.js Scene Setup
const scene = new THREE.Scene();

// Add gradient background
const canvas = document.createElement('canvas');
canvas.width = 2;
canvas.height = 256;
const context = canvas.getContext('2d');
const gradient = context.createLinearGradient(0, 0, 0, 256);
gradient.addColorStop(0, '#1e3a5f');
gradient.addColorStop(0.5, '#2d5986');
gradient.addColorStop(1, '#87ceeb');
context.fillStyle = gradient;
context.fillRect(0, 0, 2, 256);
const gradientTexture = new THREE.CanvasTexture(canvas);
scene.background = gradientTexture;

// Camera and Renderer
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 10000);
const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
document.body.appendChild(renderer.domElement);

// Orbit Controls
const controls = new THREE.OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;
controls.dampingFactor = 0.05;

// Lighting
const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
directionalLight.position.set(2000, 3000, 2000);
directionalLight.castShadow = true;
directionalLight.shadow.mapSize.width = 2048;
directionalLight.shadow.mapSize.height = 2048;
scene.add(directionalLight);

const ambientLight = new THREE.AmbientLight(0x606060);
scene.add(ambientLight);

const hemisphereLight = new THREE.HemisphereLight(0xffffbb, 0x080820, 0.5);
scene.add(hemisphereLight);

// UI Elements
const fileSelector = document.getElementById('file-selector');
const palletSelector = document.getElementById('pallet-selector');
const resetCameraButton = document.getElementById('reset-camera');
const itemCountEl = document.getElementById('item-count');
const skuCountEl = document.getElementById('sku-count');
const dimensionsEl = document.getElementById('dimensions');
const utilizationEl = document.getElementById('utilization');
const maxHeightEl = document.getElementById('max-height');

// Global State
let palletData = {};
let currentPalletGroup = new THREE.Group();
let currentPalletDimensions = { x: 2800, y: 1000, z: 1260 }; // Default from Pallet.cs
let productColors = {}; // Store colors for each ProductId
const PALLET_BASE_HEIGHT = 144; // Pallet thickness in mm

// Generate consistent color for a ProductId using a hash
function getColorForProduct(productId) {
    if (productColors[productId]) {
        return productColors[productId];
    }

    // Simple hash function to generate consistent color
    let hash = 0;
    const str = productId.toString();
    for (let i = 0; i < str.length; i++) {
        hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }

    // Generate RGB values with good saturation and brightness
    const h = Math.abs(hash % 360);
    const s = 65 + (Math.abs(hash >> 8) % 20); // 65-85% saturation
    const l = 55 + (Math.abs(hash >> 16) % 15); // 55-70% lightness

    // Convert HSL to RGB
    const c = (1 - Math.abs(2 * l / 100 - 1)) * s / 100;
    const x = c * (1 - Math.abs((h / 60) % 2 - 1));
    const m = l / 100 - c / 2;

    let r, g, b;
    if (h < 60) { r = c; g = x; b = 0; }
    else if (h < 120) { r = x; g = c; b = 0; }
    else if (h < 180) { r = 0; g = c; b = x; }
    else if (h < 240) { r = 0; g = x; b = c; }
    else if (h < 300) { r = x; g = 0; b = c; }
    else { r = c; g = 0; b = x; }

    r = Math.round((r + m) * 255);
    g = Math.round((g + m) * 255);
    b = Math.round((b + m) * 255);

    const color = (r << 16) | (g << 8) | b;
    productColors[productId] = color;
    return color;
}

// Clear scene function
function clearScene() {
    if (currentPalletGroup) {
        scene.remove(currentPalletGroup);
        currentPalletGroup.children.forEach(obj => {
            if (obj.geometry) obj.geometry.dispose();
            if (obj.material) {
                if (Array.isArray(obj.material)) {
                    obj.material.forEach(mat => mat.dispose());
                } else {
                    obj.material.dispose();
                }
            }
        });
        currentPalletGroup = new THREE.Group();
    }
}

// Load packing data from CSV
async function loadPackingData(filename) {
    clearScene();
    palletData = {};
    productColors = {}; // Reset color mapping for new file

    try {
        const response = await fetch(`/packing_data/${filename}`);
        const csvText = await response.text();
        const lines = csvText.trim().split('\n');
        const headers = lines[0].split(',');

        // Parse CSV - handle item_placements format
        // OrderId,PalletId,ItemId,ProductId,X,Y,Z,Length,Width,Height,Weight,IsRotated,PalletLength,PalletWidth,PalletMaxHeight
        for (let i = 1; i < lines.length; i++) {
            const values = lines[i].split(',');
            const item = {};
            headers.forEach((header, index) => {
                item[header.trim()] = values[index] ? values[index].trim() : '';
            });

            const palletId = item.PalletId;
            if (!palletData[palletId]) {
                // Read pallet dimensions from CSV (if available), otherwise use defaults
                const palletLength = parseFloat(item.PalletLength) || 2600;
                const palletWidth = parseFloat(item.PalletWidth) || 1000;
                const palletHeight = parseFloat(item.PalletMaxHeight) || 1260;

                palletData[palletId] = {
                    items: [],
                    dimensions: { x: palletLength, y: palletWidth, z: palletHeight }
                };
            }
            palletData[palletId].items.push(item);
        }

        console.log(`Loaded ${Object.keys(palletData).length} pallets from ${filename}`);

        // Render the first pallet
        const firstPalletId = Object.keys(palletData)[0];
        if (firstPalletId) {
            // Populate pallet selector
            palletSelector.innerHTML = '';
            Object.keys(palletData).sort().forEach(pId => {
                const option = document.createElement('option');
                option.value = pId;
                const itemCount = palletData[pId].items.length;
                option.text = `${pId} (${itemCount} items)`;
                palletSelector.appendChild(option);
            });
            palletSelector.value = firstPalletId;
            renderPallet(firstPalletId);
        }

    } catch (error) {
        console.error('Error loading packing data:', error);
        alert(`Failed to load ${filename}: ${error.message}`);
    }
}

// Fetch available CSV files
async function fetchCsvFiles() {
    try {
        const response = await fetch('/list_csv');
        const files = await response.json();

        fileSelector.innerHTML = '';
        files.sort().forEach(file => {
            const option = document.createElement('option');
            option.value = file;
            option.text = file;
            fileSelector.appendChild(option);
        });

        if (files.length > 0) {
            loadPackingData(files[0]);
        } else {
            alert('No CSV files found in PackingResults folder!');
        }
    } catch (error) {
        console.error('Error fetching CSV files:', error);
        alert('Failed to fetch CSV files. Make sure the server is running.');
    }
}

// Calculate statistics
function calculateStatistics(pallet) {
    const items = pallet.items;
    const uniqueSkus = new Set(items.map(item => item.ProductId)).size; // Use ProductId

    let totalVolume = 0;
    let maxHeight = 0;

    items.forEach(item => {
        const volume = parseFloat(item.Length) * parseFloat(item.Width) * parseFloat(item.Height);
        totalVolume += volume;

        const itemMaxZ = parseFloat(item.Z) + parseFloat(item.Height);
        maxHeight = Math.max(maxHeight, itemMaxZ);
    });

    const palletVolume = pallet.dimensions.x * pallet.dimensions.y * pallet.dimensions.z;
    const utilization = ((totalVolume / palletVolume) * 100).toFixed(1);

    return {
        itemCount: items.length,
        skuCount: uniqueSkus,
        utilization: utilization,
        maxHeight: maxHeight.toFixed(0)
    };
}

// Update statistics display
function updateStats(pallet) {
    const stats = calculateStatistics(pallet);
    itemCountEl.textContent = stats.itemCount;
    skuCountEl.textContent = stats.skuCount;
    dimensionsEl.textContent = `${pallet.dimensions.x}×${pallet.dimensions.y}×${pallet.dimensions.z}`;
    utilizationEl.textContent = `${stats.utilization}%`;
    maxHeightEl.textContent = `${stats.maxHeight} mm`;
}

// Render a specific pallet
function renderPallet(palletId) {
    clearScene();

    const pallet = palletData[palletId];
    if (!pallet) {
        console.error(`Pallet with ID ${palletId} not found.`);
        return;
    }

    currentPalletDimensions = pallet.dimensions;
    updateStats(pallet);

    // Pallet base (144mm thick platform) - 나무 팔레트 표현
    const palletGeometry = new THREE.BoxGeometry(
        pallet.dimensions.x,
        PALLET_BASE_HEIGHT,
        pallet.dimensions.y
    );

    const palletBaseMaterial = new THREE.MeshPhongMaterial({
        color: 0x8B4513,  // Saddle brown for wooden pallet
        shininess: 10
    });

    const palletMesh = new THREE.Mesh(palletGeometry, palletBaseMaterial);
    palletMesh.position.set(
        pallet.dimensions.x / 2,
        PALLET_BASE_HEIGHT / 2,
        pallet.dimensions.y / 2
    );
    palletMesh.castShadow = false;
    palletMesh.receiveShadow = true;
    currentPalletGroup.add(palletMesh);

    // Add black outline for pallet base
    const palletEdges = new THREE.EdgesGeometry(palletGeometry);
    const palletOutline = new THREE.LineSegments(
        palletEdges,
        new THREE.LineBasicMaterial({ color: 0x000000, linewidth: 2 })
    );
    palletOutline.position.copy(palletMesh.position);
    currentPalletGroup.add(palletOutline);

    // Add wireframe boundary for the packing volume (max height boundary)
    const boundaryGeometry = new THREE.BoxGeometry(
        pallet.dimensions.x,
        pallet.dimensions.z,
        pallet.dimensions.y
    );
    const boundaryEdges = new THREE.EdgesGeometry(boundaryGeometry);
    const boundaryLine = new THREE.LineSegments(
        boundaryEdges,
        new THREE.LineBasicMaterial({
            color: 0xFF6600,  // Orange for max height boundary
            linewidth: 2,
            transparent: true,
            opacity: 0.6
        })
    );
    boundaryLine.position.set(
        pallet.dimensions.x / 2,
        PALLET_BASE_HEIGHT + pallet.dimensions.z / 2,
        pallet.dimensions.y / 2
    );
    currentPalletGroup.add(boundaryLine);

    // Add ground plane for shadow
    const groundGeometry = new THREE.PlaneGeometry(5000, 5000);
    const groundMaterial = new THREE.ShadowMaterial({ opacity: 0.3 });
    const ground = new THREE.Mesh(groundGeometry, groundMaterial);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = 0;
    ground.receiveShadow = true;
    currentPalletGroup.add(ground);

    pallet.items.forEach((item, index) => {
        const itemLength = parseFloat(item.Length);
        const itemWidth = parseFloat(item.Width);
        const itemHeight = parseFloat(item.Height);
        const itemX = parseFloat(item.X);
        const itemY = parseFloat(item.Y);
        const itemZ = parseFloat(item.Z);

        // Create box geometry (THREE.js uses Y-up)
        const itemGeometry = new THREE.BoxGeometry(itemLength, itemHeight, itemWidth);

        // Use color from CSV if available, otherwise generate based on ProductId
        let itemColor;
        if (item.Color && item.Color.startsWith('#')) {
            itemColor = parseInt(item.Color.substring(1), 16);
        } else {
            itemColor = getColorForProduct(item.ProductId);
        }

        const itemMaterial = new THREE.MeshPhongMaterial({
            color: itemColor,
            shininess: 30,
            flatShading: false
        });

        const itemMesh = new THREE.Mesh(itemGeometry, itemMaterial);

        // Position: C# (X, Y, Z) -> THREE.js (X, Z-as-Y-up, Y-as-Z-depth)
        // C# coordinate system: X=length, Y=width, Z=height
        // THREE.js: X=X, Y=height, Z=depth
        itemMesh.position.set(
            itemX + itemLength / 2,
            PALLET_BASE_HEIGHT + itemZ + itemHeight / 2,
            itemY + itemWidth / 2
        );

        itemMesh.castShadow = true;
        itemMesh.receiveShadow = true;
        currentPalletGroup.add(itemMesh);

        // Add black wireframe edges
        const itemEdges = new THREE.EdgesGeometry(itemGeometry);
        const itemLine = new THREE.LineSegments(
            itemEdges,
            new THREE.LineBasicMaterial({ color: 0x000000, linewidth: 1 })
        );
        itemLine.position.copy(itemMesh.position);
        currentPalletGroup.add(itemLine);
    });

    scene.add(currentPalletGroup);

    // Reset camera position
    resetCamera();

    console.log(`Rendered pallet ${palletId} with ${pallet.items.length} items`);
}

// Reset camera to default view
function resetCamera() {
    const centerX = currentPalletDimensions.x / 2;
    const centerY = currentPalletDimensions.z / 2;
    const centerZ = currentPalletDimensions.y / 2;

    camera.position.set(
        currentPalletDimensions.x * 1.8,
        currentPalletDimensions.z * 2,
        currentPalletDimensions.y * 1.8
    );

    camera.lookAt(centerX, centerY, centerZ);
    controls.target.set(centerX, centerY, centerZ);
    controls.update();
}

// Event listeners
fileSelector.addEventListener('change', (event) => {
    loadPackingData(event.target.value);
});

palletSelector.addEventListener('change', (event) => {
    renderPallet(event.target.value);
});

resetCameraButton.addEventListener('click', () => {
    resetCamera();
});

// Handle window resize
window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
});

// Animation loop
function animate() {
    requestAnimationFrame(animate);
    controls.update();
    renderer.render(scene, camera);
}

// Initialize
fetchCsvFiles();
animate();
