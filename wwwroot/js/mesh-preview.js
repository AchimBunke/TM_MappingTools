// mesh-preview.js  –  Three.js scene manager for NormalizedMesh preview
import * as THREE from 'https://esm.sh/three@0.170.0';
import { OrbitControls } from 'https://esm.sh/three@0.170.0/examples/jsm/controls/OrbitControls';

// shape-like types should always render as wireframe
const SHAPE_TYPES = new Set([1, 2, 3, 4]);

const SEL_COLOR   = 0x00eeff;
const SEL_EMISSIVE = 0x004455;

// ─── config-driven colour ─────────────────────────────────────────────────────
//   movable    → teal  (#33ccbb)
//   trigger    → pink  (#ff4488)
//   collidable → gold  (#ddaa22)
//   default    → blue  (#4488ff)
function configColor(config) {
    if (config.movable)    return 0x33cc91;
    if (config.trigger)    return 0xff4488;
    if (config.collidable) return 0xddaa22;
    return 0x4488ff;
}

// canvas → preview-state  map
const previews = new Map();

// ─── theme helpers ───────────────────────────────────────────────────────────

function resolveCSSColor(varName, fallback) {
    const v = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    return v || fallback;
}

function syncPreviewTheme(preview) {
    const bg         = resolveCSSColor('--tm-bg',         '#0f1623');
    const gridCenter = resolveCSSColor('--tm-text-muted', '#7a93b2');
    const gridLines  = resolveCSSColor('--tm-border',     '#253149');

    preview.scene.background = new THREE.Color(bg);

    // GridHelper bakes vertex colours – rebuild it to pick up new palette
    const old = preview.grid;
    const py = old.position.y, sx = old.scale.x, sz = old.scale.z;
    preview.scene.remove(old);
    old.geometry?.dispose();
    (Array.isArray(old.material) ? old.material : [old.material]).forEach(m => m?.dispose());
    const grid = new THREE.GridHelper(20, 20, new THREE.Color(gridCenter), new THREE.Color(gridLines));
    grid.position.y = py;
    grid.scale.set(sx, 1, sz);
    preview.scene.add(grid);
    preview.grid = grid;
}

// Re-sync all live previews whenever [data-theme] changes
new MutationObserver(() => {
    for (const p of previews.values()) syncPreviewTheme(p);
}).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

// ─── helpers ─────────────────────────────────────────────────────────────────

function buildMaterial(type) {
    const isShape = SHAPE_TYPES.has(type);
    if (isShape) {
        return new THREE.MeshStandardMaterial({
            color: 0x4488ff,
            wireframe: true,
            transparent: true,
            opacity: 0.75,
            side: THREE.DoubleSide,
        });
    }
    return new THREE.MeshStandardMaterial({
        color: 0x4488ff,
        roughness: 0.65,
        metalness: 0.05,
        side: THREE.DoubleSide,
    });
}

// brighter colours used when wireframe mode is active on normally-solid meshes,
// so they visually stand apart from always-wireframe collision shapes.
function configWireframeColor(config) {
    if (config.movable)    return 0x55eedd;
    if (config.trigger)    return 0xff99bb;
    if (config.collidable) return 0xffcc55;
    return 0x99bbff;
}

function applyConfigs(preview) {
    const { submeshObjects, selectedIndex, wireframeMode } = preview;
    for (const sub of submeshObjects) {
        const { mesh, wireEdges, index, type, config } = sub;
        if (!mesh) continue;

        const isSelected = index === selectedIndex;
        const isShape    = SHAPE_TYPES.has(type);
        const isGhost    = !config.visible;
        const isVisibleInLOD = (preview.visibleLODMask & config.lodMask) !== 0;  
              
        if (!config.enabled) {
            mesh.visible = false;
            if (wireEdges) wireEdges.visible = false;
            continue;
        }
        mesh.visible = isVisibleInLOD;

        const mat = mesh.material;

        if (isSelected) {
            mat.color.setHex(SEL_COLOR);
            if (mat.emissive) mat.emissive.setHex(SEL_EMISSIVE);
            mat.wireframe   = false;
            mat.transparent = true;
            mat.opacity     = 1.0;
            mat.needsUpdate = true;
            if (wireEdges) wireEdges.visible = false;
            continue;
        }

        mat.color.setHex(configColor(config));
        if (mat.emissive) mat.emissive.setHex(0x000000);

        // Shapes and ghost meshes are always wireframe material.
        // Mode 1 (wireframe) also forces material wireframe on solid meshes.
        const wantMaterialWireframe = isShape || isGhost || preview.renderMode === 1;
        mat.wireframe   = wantMaterialWireframe;
        mat.transparent = wantMaterialWireframe;
        mat.opacity     = wantMaterialWireframe ? (isGhost ? 0.50 : 0.70) : 1.0;
        mat.needsUpdate = true;

        // Edge overlay: mode 2 only, on solid visible meshes.
        if (wireEdges) {
            const showEdges = preview.renderMode === 2 && !isShape && !isGhost;
            wireEdges.visible = showEdges;
            if (showEdges) wireEdges.material.color.setHex(configWireframeColor(config));
        }
    }
}

// ─── public API ──────────────────────────────────────────────────────────────

export function initMeshPreview(canvas, dotNetRef) {
    if (previews.has(canvas)) disposeMeshPreview(canvas);

    // renderer
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.setSize(canvas.clientWidth || 400, canvas.clientHeight || 300, false);

    // scene
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(resolveCSSColor('--tm-bg', '#0f1623'));

    // lights
    const ambient = new THREE.AmbientLight(0xffffff, 0.45);
    scene.add(ambient);
    const sun = new THREE.DirectionalLight(0xffffff, 1.3);
    sun.position.set(6, 12, 8);
    scene.add(sun);
    const fill = new THREE.DirectionalLight(0x88aaff, 0.35);
    fill.position.set(-5, -2, -6);
    scene.add(fill);

    // camera
    const camera = new THREE.PerspectiveCamera(55, (canvas.clientWidth || 4) / (canvas.clientHeight || 3), 0.01, 100000);
    camera.position.set(4, 3, 6);

    // controls
    const controls = new OrbitControls(camera, canvas);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.screenSpacePanning = false;

    // grid (positioned at y=0 until first mesh load)
    const grid = new THREE.GridHelper(20, 20,
        new THREE.Color(resolveCSSColor('--tm-text-muted', '#7a93b2')),
        new THREE.Color(resolveCSSColor('--tm-border',     '#253149')));
    scene.add(grid);

    // mesh group
    const meshGroup = new THREE.Group();
    scene.add(meshGroup);

    // raycasting
    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();

    const preview = {
        renderer, scene, camera, controls,
        meshGroup, grid,
        submeshObjects: [],   // { mesh, index, type, config }
        selectedIndex: null,
        wireframeMode: false,
        renderMode: 0,        // 0=solid  1=wireframe  2=solid+edges
        visibleLODMask: 0xFFFFFFFF,
        dotNetRef,
        animFrameId: null,
        resizeObserver: null,
        onClick: null,
        onPointerDown: null,
        _pointerDownPos: null,
    };

    // track pointer-down position to distinguish click from drag
    preview.onPointerDown = (e) => { preview._pointerDownPos = { x: e.clientX, y: e.clientY }; };
    canvas.addEventListener('pointerdown', preview.onPointerDown);

    // click → select submesh (ignored if pointer moved > 5 px = drag)
    preview.onClick = (e) => {
        const start = preview._pointerDownPos;
        if (start) {
            const dx = e.clientX - start.x;
            const dy = e.clientY - start.y;
            if (dx * dx + dy * dy > 25) return;
        }
        const rect = canvas.getBoundingClientRect();
        pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
        pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
        raycaster.setFromCamera(pointer, camera);

        const meshes = preview.submeshObjects.map(s => s.mesh).filter(Boolean);
        const hits = raycaster.intersectObjects(meshes, false);
        let newIndex = null;
        if (hits.length > 0) {
            const found = preview.submeshObjects.find(s => s.mesh === hits[0].object);
            if (found) newIndex = found.index === preview.selectedIndex ? null : found.index;
        }
        preview.selectedIndex = newIndex;
        applyConfigs(preview);
        dotNetRef.invokeMethodAsync('OnSubmeshSelected', newIndex ?? -1);
    };
    canvas.addEventListener('click', preview.onClick);

    // resize observer
    const container = canvas.parentElement ?? canvas;
    preview.resizeObserver = new ResizeObserver(() => {
        const w = container.clientWidth;
        const h = container.clientHeight;
        if (w > 0 && h > 0) {
            renderer.setSize(w, h, false);
            camera.aspect = w / h;
            camera.updateProjectionMatrix();
        }
    });
    preview.resizeObserver.observe(container);

    // render loop
    function animate() {
        preview.animFrameId = requestAnimationFrame(animate);
        controls.update();
        renderer.render(scene, camera);
    }
    animate();

    previews.set(canvas, preview);
}

export function updateMesh(canvas, submeshes) {
    const preview = previews.get(canvas);
    if (!preview) return;

    // clear old objects
    for (const sub of preview.submeshObjects) {
        if (sub.mesh) {
            sub.mesh.geometry?.dispose();
            sub.mesh.material?.dispose();
            preview.meshGroup.remove(sub.mesh);
        }
        if (sub.wireEdges) {
            sub.wireEdges.geometry?.dispose();
            sub.wireEdges.material?.dispose();
            preview.meshGroup.remove(sub.wireEdges);
        }
    }
    preview.submeshObjects = [];
    preview.selectedIndex = null;

    const sceneBounds = new THREE.Box3();
    let anyGeom = false;

    for (let i = 0; i < submeshes.length; i++) {
        const sub = submeshes[i];
        const positions = sub.positions;
        const indices = sub.indices;
        if (!positions || positions.length === 0 || !indices || indices.length === 0) continue;

        const geom = new THREE.BufferGeometry();
        geom.setAttribute('position', new THREE.BufferAttribute(new Float32Array(positions), 3));
        if (sub.normals && sub.normals.length === positions.length) {
            geom.setAttribute('normal', new THREE.BufferAttribute(new Float32Array(sub.normals), 3));
        }
        geom.setIndex(indices);
        if (!sub.normals || sub.normals.length === 0) geom.computeVertexNormals();

        const mat = buildMaterial(sub.type);
        const mesh = new THREE.Mesh(geom, mat);
        preview.meshGroup.add(mesh);

        // edge overlay (shown when wireframe mode is on for solid visible meshes)
        const wireGeom  = new THREE.WireframeGeometry(geom);
        const wireMat   = new THREE.LineBasicMaterial({ color: 0x99bbff, transparent: true, opacity: 0.55 });
        const wireEdges = new THREE.LineSegments(wireGeom, wireMat);
        wireEdges.visible = false;
        preview.meshGroup.add(wireEdges);

        preview.submeshObjects.push({
            mesh,
            wireEdges,
            index: i,
            type: sub.type,
            config: {
                enabled: sub.enabled,
                visible: sub.visible,
                collidable: sub.collidable,
                trigger: sub.trigger,
                movable: sub.movable,
                lodMask: sub.lodMask,
            },
        });

        geom.computeBoundingBox();
        if (geom.boundingBox) { sceneBounds.union(geom.boundingBox); anyGeom = true; }
    }

    applyConfigs(preview);

    // fit camera
    if (anyGeom) {
        const center = new THREE.Vector3();
        const size = new THREE.Vector3();
        sceneBounds.getCenter(center);
        sceneBounds.getSize(size);
        const maxDim = Math.max(size.x, size.y, size.z, 0.01);
        const fov = preview.camera.fov * (Math.PI / 180);
        const dist = (maxDim / 2) / Math.tan(fov / 2) * 1.9;

        preview.camera.position.set(
            center.x + dist * 0.55,
            center.y + dist * 0.45,
            center.z + dist * 0.80
        );
        preview.controls.target.copy(center);
        preview.controls.update();
        preview.camera.near = maxDim * 0.001;
        preview.camera.far = maxDim * 200;
        preview.camera.updateProjectionMatrix();

        preview.grid.position.y = sceneBounds.min.y;
        const gridScale = Math.max(maxDim * 2, 5);
        preview.grid.scale.set(gridScale / 20, 1, gridScale / 20);
    }
}

export function updateConfigs(canvas, configs) {
    const preview = previews.get(canvas);
    if (!preview) return;
    for (const sub of preview.submeshObjects) {
        const cfg = configs[sub.index];
        if (cfg !== undefined && cfg !== null) sub.config = cfg;
    }
    applyConfigs(preview);
}

export function highlightSubmesh(canvas, index) {
    const preview = previews.get(canvas);
    if (!preview) return;
    preview.selectedIndex = (index >= 0) ? index : null;
    applyConfigs(preview);
}

export function addEscapeListener(dotNetRef) {
    function handler(e) {
        if (e.key === 'Escape') {
            document.removeEventListener('keydown', handler);
            dotNetRef.invokeMethodAsync('CollapsePreview');
        }
    }
    document.addEventListener('keydown', handler);
}

export function setRenderMode(canvas, mode) {
    const preview = previews.get(canvas);
    if (!preview) return;
    preview.renderMode = mode;
    applyConfigs(preview);
}

export function setLODVisibility(canvas, visibleLODMask) {
    const preview = previews.get(canvas);
    if (!preview) return;
    preview.visibleLODMask = visibleLODMask;
    applyConfigs(preview);
}

export function initSettingsSliders(canvas, rotateEl, zoomEl, panEl) {
    const preview = previews.get(canvas);

    function syncAndAttach(el, getter, setter) {
        // set slider position and label from current OrbitControls value
        const cur = getter(preview?.controls);
        if (cur !== undefined) {
            el.value = cur;
            const label = el.closest('.settings-row')?.querySelector('.settings-val');
            if (label) label.textContent = cur.toFixed(1);
        }
        el.addEventListener('input', () => {
            const v = parseFloat(el.value);
            if (isNaN(v)) return;
            const label = el.closest('.settings-row')?.querySelector('.settings-val');
            if (label) label.textContent = v.toFixed(1);
            if (preview) setter(preview.controls, v);
        });
    }

    syncAndAttach(rotateEl, c => c?.rotateSpeed, (c, v) => c.rotateSpeed = v);
    syncAndAttach(zoomEl,   c => c?.zoomSpeed,   (c, v) => c.zoomSpeed   = v);
    syncAndAttach(panEl,    c => c?.panSpeed,     (c, v) => c.panSpeed    = v);
}

export function updateCameraSettings(canvas, rotateSpeed, zoomSpeed, panSpeed) {
    const preview = previews.get(canvas);
    if (!preview) return;
    preview.controls.rotateSpeed = rotateSpeed;
    preview.controls.zoomSpeed   = zoomSpeed;
    preview.controls.panSpeed    = panSpeed;
}

export function disposeMeshPreview(canvas) {
    const preview = previews.get(canvas);
    if (!preview) return;

    cancelAnimationFrame(preview.animFrameId);
    preview.resizeObserver?.disconnect();
    canvas.removeEventListener('pointerdown', preview.onPointerDown);
    canvas.removeEventListener('click', preview.onClick);

    for (const sub of preview.submeshObjects) {
        sub.mesh?.geometry?.dispose();
        sub.mesh?.material?.dispose();
        sub.wireEdges?.geometry?.dispose();
        sub.wireEdges?.material?.dispose();
    }
    preview.renderer.dispose();
    previews.delete(canvas);
}
