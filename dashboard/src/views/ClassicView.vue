<script setup lang="ts">
// Monta o interface clássico completo (App.vue original) para que
// nenhuma funcionalidade existente se perca. A sessão é partilhada
// via localStorage, por isso o utilizador autenticado é reconhecido.
import ClassicApp from '@/App.vue'
</script>

<template>
  <ClassicApp />
</template>
