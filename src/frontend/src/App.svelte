<script>
  import { onMount } from 'svelte';
  import { fade } from 'svelte/transition';

  let currentImage = "";
  let nextImage = "";
  let loading = false;
  let transitioning = false;

  const API_URL = "https://konachan.net/post.json?limit=1&tags=order:random+rating:safe";

  async function fetchWaifu() {
    if (loading || transitioning) return;
    loading = true;
    
    try {
      const proxyJsonUrl = `/api/proxy?url=${encodeURIComponent(API_URL)}`;
      const res = await fetch(proxyJsonUrl);
      const data = await res.json();
      
      if (data && data.length > 0) {
        const newUrl = `/api/proxy?url=${encodeURIComponent(data[0].file_url)}`;
        
        // Preload image
        const img = new Image();
        img.src = newUrl;
        img.onload = () => {
          loading = false;
          transitioning = true;
          
          // Set the next image
          nextImage = newUrl;
          
          // After a short delay (or using on:introend), swap them
          // Svelte transitions will handle the crossfade
          setTimeout(() => {
            currentImage = nextImage;
            nextImage = "";
            transitioning = false;
          }, 1500); // Match transition duration
        };
        img.onerror = () => {
          loading = false;
        };
      }
    } catch (error) {
      console.error("Failed to fetch waifu:", error);
      loading = false;
    }
  }

  onMount(() => {
    fetchWaifu();
  });

  function handleKeydown(e) {
    if (e.code === 'Space') fetchWaifu();
  }
</script>

<svelte:window on:keydown={handleKeydown} />

<main>
  <div class="container">
    <!-- Current/Old Image -->
    {#if currentImage}
      <img 
        src={currentImage} 
        alt="" 
        class="wallpaper" 
        draggable="false"
        out:fade={{ duration: 1200 }}
      />
    {/if}

    <!-- New Image Fading In -->
    {#if nextImage}
      <img 
        src={nextImage} 
        alt="" 
        class="wallpaper" 
        draggable="false"
        in:fade={{ duration: 1200 }}
      />
    {/if}

    <div class="controls">
      <div class="button-wrapper">
        {#if loading}
          <div class="spinner-overlay" in:fade out:fade></div>
        {/if}
        
        <button class="random-btn" on:click|stopPropagation={fetchWaifu} disabled={loading || transitioning} title="Random Wallpaper">
          <span class="text">↻</span>
        </button>
      </div>
    </div>
  </div>
</main>

<style lang="scss">
  :global(body) {
    margin: 0;
    padding: 0;
    overflow: hidden;
    background-color: #000;
    user-select: none;
  }

  main {
    width: 100vw;
    height: 100vh;
    position: relative;
    background: #000;
  }

  .container {
    width: 100%;
    height: 100%;
    position: relative;
  }

  .wallpaper {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
    -webkit-user-drag: none;
    user-drag: none;
  }

  .controls {
    position: absolute;
    bottom: 40px;
    right: 40px;
    z-index: 9999;
  }

  .button-wrapper {
    position: relative;
    width: 64px;
    height: 64px;
    display: flex;
    justify-content: center;
    align-items: center;
  }

  .spinner-overlay {
    position: absolute;
    width: 64px;
    height: 64px;
    border: 3px solid transparent;
    border-top: 3px solid #ffffff;
    border-radius: 50%;
    animation: spin 1s cubic-bezier(0.5, 0, 0.5, 1) infinite;
    pointer-events: none;
    z-index: 10;
  }

  .random-btn {
    background: rgba(0, 0, 0, 0.6);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: white;
    width: 56px;
    height: 56px;
    border-radius: 50%;
    font-size: 1.8rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.3s ease;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
    pointer-events: auto;
    z-index: 2;

    &:hover:not(:disabled) {
      background: rgba(0, 0, 0, 0.8);
      border-color: rgba(255, 255, 255, 0.2);
      transform: scale(1.1);
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .text {
        line-height: 1;
        margin-top: -2px;
    }
  }

  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }
</style>